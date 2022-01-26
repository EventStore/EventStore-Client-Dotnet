using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	public class GossipChannelSelectorTests {
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

			await using var channelCache = new ChannelCache(settings);
			var sut = new GossipChannelSelector(settings, channelCache, new FakeGossipClient(
					new ClusterMessages.ClusterInfo(
						new ClusterMessages.MemberInfo[] {
							new(firstId, ClusterMessages.VNodeState.Leader, true, firstSelection),
							new(secondId, ClusterMessages.VNodeState.Follower, true, secondSelection),
						})));

			var channel = await sut.SelectChannelAsync(cancellationToken: default);
			Assert.Equal($"{firstSelection.Host}:{firstSelection.Port}", channel.Target);

			channel = sut.SelectChannel(secondSelection);
			Assert.Equal($"{secondSelection.Host}:{secondSelection.Port}", channel.Target);
		}

		[Fact]
		public async Task ThrowsWhenDiscoveryFails() {
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					IpGossipSeeds = new[] {new IPEndPoint(IPAddress.Loopback, 2113)},
					Insecure = true,
					MaxDiscoverAttempts = 3
				}
			};

			await using var channelCache = new ChannelCache(settings);
			var sut = new GossipChannelSelector(settings, channelCache, new BadGossipClient());

			var ex = await Assert.ThrowsAsync<DiscoveryException>(async () =>
				await sut.SelectChannelAsync(cancellationToken: default).ConfigureAwait(false));

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
	}
}
