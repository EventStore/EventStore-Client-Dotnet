using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;
namespace EventStore.Client {
	public class GossipChannelSelectorTests {
		private static readonly ClusterMessages.VNodeState[] NotAllowedStates = {
			ClusterMessages.VNodeState.Manager,
			ClusterMessages.VNodeState.ShuttingDown,
			ClusterMessages.VNodeState.Shutdown,
			ClusterMessages.VNodeState.Unknown,
			ClusterMessages.VNodeState.Initializing,
			ClusterMessages.VNodeState.CatchingUp,
			ClusterMessages.VNodeState.ResigningLeader,
			ClusterMessages.VNodeState.PreLeader,
			ClusterMessages.VNodeState.PreReplica,
			ClusterMessages.VNodeState.Clone,
			ClusterMessages.VNodeState.DiscoverLeader
		};

		public static IEnumerable<object[]> InvalidStatesCases() {
			foreach (var state in NotAllowedStates) {
				var allowedNodeId = Uuid.NewUuid();
				var allowedNode = new DnsEndPoint(allowedNodeId.ToString(), 2113);

				var notAllowedNodeId = Uuid.NewUuid();
				var notAllowedNode = new DnsEndPoint(notAllowedNodeId.ToString(), 2114);

				var settings = new EventStoreClientSettings {
					ConnectivitySettings = {
						DnsGossipSeeds = new[] {allowedNode, notAllowedNode},
						Insecure = true
					}
				};

				yield return new object[] {
					new ClusterMessages.ClusterInfo(new ClusterMessages.MemberInfo[] {
						new(allowedNodeId, ClusterMessages.VNodeState.Leader, true, allowedNode),
						new(notAllowedNodeId, state, true, notAllowedNode),
					}),
					settings,
					allowedNode
				};
			}
		}

		[Theory, MemberData(nameof(InvalidStatesCases))]
		internal async Task InvalidStatesAreNotConsidered(ClusterMessages.ClusterInfo clusterInfo,
			EventStoreClientSettings settings, DnsEndPoint allowedNode) {
			await using var sut = new GossipChannelSelector(settings, new FakeGossipClient(clusterInfo));

			var (channel, _) = await sut.SelectChannel();
			Assert.Equal($"{allowedNode.Host}:{allowedNode.Port}", channel.Target);
		}

		[Fact]
		public async Task DeadNodesAreNotConsidered() {
			var allowedNodeId = Uuid.NewUuid();
			var allowedNode = new DnsEndPoint(allowedNodeId.ToString(), 2113);

			var notAllowedNodeId = Uuid.NewUuid();
			var notAllowedNode = new DnsEndPoint(notAllowedNodeId.ToString(), 2114);

			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					DnsGossipSeeds = new[] {allowedNode, notAllowedNode},
					Insecure = true
				}
			};

			await using var sut = new GossipChannelSelector(settings, new FakeGossipClient(
					new ClusterMessages.ClusterInfo(
						new ClusterMessages.MemberInfo[] {
							new(allowedNodeId, ClusterMessages.VNodeState.Follower, true, allowedNode),
							new(notAllowedNodeId, ClusterMessages.VNodeState.Leader, false, notAllowedNode),
						})));

			var (channel, _) = await sut.SelectChannel();
			Assert.Equal($"{allowedNode.Host}:{allowedNode.Port}", channel.Target);
		}

		[Fact]
		public async Task ExplicitlySettingEndPointChangesChannels() {
			var firstId = Uuid.NewUuid();
			var secondId = Uuid.NewUuid();

			var firstSelection = new DnsEndPoint(firstId.ToString(), 2113);
			var secondSelection = new DnsEndPoint(secondId.ToString(), 2113);

			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					DnsGossipSeeds = new[] {firstSelection, secondSelection},
					Insecure = true
				}
			};

			await using var sut = new GossipChannelSelector(settings, new FakeGossipClient(
					new ClusterMessages.ClusterInfo(
						new ClusterMessages.MemberInfo[] {
							new(firstId, ClusterMessages.VNodeState.Leader, true, firstSelection),
							new(secondId, ClusterMessages.VNodeState.Follower, true, secondSelection),
						})));

			var (channel, _) = await sut.SelectChannel();
			Assert.Equal($"{firstSelection.Host}:{firstSelection.Port}", channel.Target);

			sut.Rediscover(secondSelection);

			(channel, _) = await sut.SelectChannel();
			Assert.Equal($"{secondSelection.Host}:{secondSelection.Port}", channel.Target);
		}

		[Fact]
		public async Task ThrowsWhenDiscoveryFails() {
			var sut = new GossipChannelSelector(new EventStoreClientSettings {
				ConnectivitySettings = {
					IpGossipSeeds = new[] {new IPEndPoint(IPAddress.Loopback, 2113)},
					Insecure = true,
					MaxDiscoverAttempts = 3
				}
			}, new BadGossipClient());

			var ex = await Assert.ThrowsAsync<DiscoveryException>(async () =>
				await sut.SelectChannel().ConfigureAwait(false));

			Assert.Equal(3, ex.MaxDiscoverAttempts);
		}

		private class FakeGossipClient : IGossipClient {
			private readonly ClusterMessages.ClusterInfo _clusterInfo;

			public FakeGossipClient(ClusterMessages.ClusterInfo clusterInfo) {
				_clusterInfo = clusterInfo;
			}

			public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel,
				CancellationToken cancellationToken) => new(_clusterInfo);
		}

		private class BadGossipClient : IGossipClient {
			public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel,
				CancellationToken cancellationToken) {
				throw new NotSupportedException();
			}
		}

		private class FakeServerCapabilitiesClient : IServerCapabilitiesClient {
			public ValueTask<ServerCapabilitiesInfo> GetAsync(ChannelBase channel, CancellationToken cancellationToken) =>
				new(new ServerCapabilitiesInfo());
		}
	}
}
