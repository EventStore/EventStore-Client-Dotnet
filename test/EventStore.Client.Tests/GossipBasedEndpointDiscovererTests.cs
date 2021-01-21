using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EndPoint = System.Net.EndPoint;

#nullable enable
namespace EventStore.Client {
	public class GossipBasedEndpointDiscovererTests {
		private readonly DnsEndPoint _gossipSeed;

		public GossipBasedEndpointDiscovererTests() {
			_gossipSeed = new DnsEndPoint("localhost", 443);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task should_issue_gossip_to_gossip_seed(bool useHttps) {
			var endPoint = new DnsEndPoint(IPAddress.Any.ToString(), 4444);
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						EndPoint = endPoint,
						IsAlive = true,
					},
				}
			};

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 1,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = NodePreference.Leader,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new CallbackTestGossipClient(gossip));

			var result = await sut.DiscoverAsync();
			Assert.Equal(result, endPoint);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task should_be_able_to_discover_twice(bool useHttps) {
			var firstGossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 1111),
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 2222),
						IsAlive = true,
					},
				}
			};
			var secondGossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 1111),
						IsAlive = false,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 2222),
						IsAlive = true,
					},
				}
			};

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 5,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = NodePreference.Leader,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new MultiGossipCallback(firstGossip, secondGossip));

			var result = await sut.DiscoverAsync();

			var expected = firstGossip.Members.First(x => x.EndPoint.Port == 1111);

			Assert.Equal(expected.EndPoint.Host, result.GetHost());
			Assert.Equal(expected.EndPoint.Port, result.GetPort());

			result = await sut.DiscoverAsync();

			expected = secondGossip.Members.First(x => x.EndPoint.Port == 2222);

			Assert.Equal(expected.EndPoint.Host, result.GetHost());
			Assert.Equal(expected.EndPoint.Port, result.GetPort());
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task should_not_exceed_max_discovery_attempts(bool useHttps) {
			int maxDiscoveryAttempts = 5;

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 5,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = NodePreference.Leader,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new CallbackTestGossipClient(new ClusterMessages.ClusterInfo(), () => throw new Exception()));

			var result = await Assert.ThrowsAsync<DiscoveryException>(async () => await sut.DiscoverAsync());

			Assert.Equal(maxDiscoveryAttempts, result.MaxDiscoverAttempts);
		}

		[Theory,
		 InlineData(true, ClusterMessages.VNodeState.Manager),
		 InlineData(true, ClusterMessages.VNodeState.Shutdown),
		 InlineData(true, ClusterMessages.VNodeState.Unknown),
		 InlineData(true, ClusterMessages.VNodeState.Initializing),
		 InlineData(true, ClusterMessages.VNodeState.CatchingUp),
		 InlineData(true, ClusterMessages.VNodeState.ResigningLeader),
		 InlineData(true, ClusterMessages.VNodeState.ShuttingDown),
		 InlineData(true, ClusterMessages.VNodeState.PreLeader),
		 InlineData(true, ClusterMessages.VNodeState.PreReplica),
		 InlineData(true, ClusterMessages.VNodeState.PreReadOnlyReplica),
		 InlineData(false, ClusterMessages.VNodeState.Manager),
		 InlineData(false, ClusterMessages.VNodeState.Shutdown),
		 InlineData(false, ClusterMessages.VNodeState.Unknown),
		 InlineData(false, ClusterMessages.VNodeState.Initializing),
		 InlineData(false, ClusterMessages.VNodeState.CatchingUp),
		 InlineData(false, ClusterMessages.VNodeState.ResigningLeader),
		 InlineData(false, ClusterMessages.VNodeState.ShuttingDown),
		 InlineData(false, ClusterMessages.VNodeState.PreLeader),
		 InlineData(false, ClusterMessages.VNodeState.PreReplica),
		 InlineData(false, ClusterMessages.VNodeState.PreReadOnlyReplica)
		]
		internal async Task should_not_be_able_to_pick_invalid_node(bool useHttps,
			ClusterMessages.VNodeState invalidState) {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = invalidState,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 4444),
						IsAlive = true,
					},
				}
			};

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 1,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = NodePreference.Leader,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new CallbackTestGossipClient(gossip));

			var ex = await Assert.ThrowsAsync<DiscoveryException>(async () => await sut.DiscoverAsync());
			Assert.Equal(1, ex.MaxDiscoverAttempts);
		}

		[Theory,
		 InlineData(true, NodePreference.Leader, ClusterMessages.VNodeState.Leader),
		 InlineData(true, NodePreference.Follower, ClusterMessages.VNodeState.Follower),
		 InlineData(true, NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyReplica),
		 InlineData(true, NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyLeaderless),
		 InlineData(false, NodePreference.Leader, ClusterMessages.VNodeState.Leader),
		 InlineData(false, NodePreference.Follower, ClusterMessages.VNodeState.Follower),
		 InlineData(false, NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyReplica),
		 InlineData(false, NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyLeaderless)
		]
		internal async Task should_pick_node_based_on_preference(bool useHttps, NodePreference preference,
			ClusterMessages.VNodeState expectedState) {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 1111),
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 2222),
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = expectedState == ClusterMessages.VNodeState.ReadOnlyLeaderless
							? expectedState
							: ClusterMessages.VNodeState.ReadOnlyReplica,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 3333),
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Manager,
						InstanceId = Guid.NewGuid(), EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 4444),
						IsAlive = true,
					},
				}
			};

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 1,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = preference,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new CallbackTestGossipClient(gossip));

			var result = await sut.DiscoverAsync();
			Assert.Equal(result.GetPort(),
				gossip.Members.Last(x => x.State == expectedState).EndPoint.Port);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task falls_back_to_first_alive_node_if_a_preferred_node_is_not_found(bool useHttps) {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 1111),
						IsAlive = false,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(),
						EndPoint = new DnsEndPoint(IPAddress.Any.ToString(), 2222),
						IsAlive = true,
					},
				}
			};

			var sut = new GossipBasedEndpointDiscoverer(
				new EventStoreClientConnectivitySettings {
					MaxDiscoverAttempts = 1,
					GossipTimeout = Timeout.InfiniteTimeSpan,
					Insecure = !useHttps,
					DiscoveryInterval = TimeSpan.Zero,
					NodePreference = NodePreference.Leader,
					DnsGossipSeeds = new[] {_gossipSeed}
				}, new CallbackTestGossipClient(gossip));

			var result = await sut.DiscoverAsync();
			Assert.Equal(result.GetPort(),
				gossip.Members.Last(x => x.State == ClusterMessages.VNodeState.Follower).EndPoint.Port);
		}

		private class CallbackTestGossipClient : IGossipClient {
			private readonly ClusterMessages.ClusterInfo _gossip;
			private readonly Action? _callback;

			public CallbackTestGossipClient(ClusterMessages.ClusterInfo gossip, Action? callback = null) {
				_gossip = gossip;
				_callback = callback;
			}


			public ValueTask<ClusterMessages.ClusterInfo> GetAsync(EndPoint endPoint,
				CancellationToken cancellationToken = default) {
				_callback?.Invoke();
				return new ValueTask<ClusterMessages.ClusterInfo>(_gossip);
			}
		}

		private class MultiGossipCallback : IGossipClient {
			private readonly ClusterMessages.ClusterInfo[] _gossip;
			private int _currentIndex;

			public MultiGossipCallback(params ClusterMessages.ClusterInfo[] gossip) {
				_gossip = gossip;
				_currentIndex = -1;
			}

			public ValueTask<ClusterMessages.ClusterInfo> GetAsync(EndPoint endPoint,
				CancellationToken cancellationToken = default) =>
				new ValueTask<ClusterMessages.ClusterInfo>(
					_gossip[Interlocked.Increment(ref _currentIndex) % _gossip.Length]);
		}
	}
}
