using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EndPoint = System.Net.EndPoint;

#nullable enable
namespace EventStore.Client {
	internal class GossipBasedEndpointDiscoverer : IEndpointDiscoverer {
		private static readonly ClusterMessages.VNodeState[] _notAllowedStates = {
			ClusterMessages.VNodeState.Manager,
			ClusterMessages.VNodeState.ShuttingDown,
			ClusterMessages.VNodeState.Manager,
			ClusterMessages.VNodeState.Shutdown,
			ClusterMessages.VNodeState.Unknown,
			ClusterMessages.VNodeState.Initializing,
			ClusterMessages.VNodeState.CatchingUp,
			ClusterMessages.VNodeState.ResigningLeader,
			ClusterMessages.VNodeState.ShuttingDown,
			ClusterMessages.VNodeState.PreLeader,
			ClusterMessages.VNodeState.PreReplica,
			ClusterMessages.VNodeState.PreReadOnlyReplica,
			ClusterMessages.VNodeState.Clone,
			ClusterMessages.VNodeState.DiscoverLeader
		};

		private readonly EventStoreClientConnectivitySettings _settings;
		private readonly IGossipClient _gossipClient;

		private ClusterMessages.ClusterInfo? _oldGossip;

		public GossipBasedEndpointDiscoverer(EventStoreClientConnectivitySettings settings,
			IGossipClient gossipClient) {
			_gossipClient = gossipClient;
			_settings = settings;
		}

		public async Task<EndPoint> DiscoverAsync(CancellationToken cancellationToken = default) {
			for (int attempt = 1; attempt <= _settings.MaxDiscoverAttempts; ++attempt) {
				try {
					var endpoint = await DiscoverEndpointAsync(cancellationToken).ConfigureAwait(false);
					if (endpoint != null) {
						return endpoint;
					}
				} catch {
				}

				await Task.Delay(_settings.DiscoveryInterval, cancellationToken).ConfigureAwait(false);
			}

			throw new DiscoveryException(_settings.MaxDiscoverAttempts);
		}

		private async Task<EndPoint?> DiscoverEndpointAsync(CancellationToken cancellationToken) {
			var oldGossip = Interlocked.Exchange(ref _oldGossip, null);
			var gossipCandidates = oldGossip?.Members != null
				? ArrangeGossipCandidates(oldGossip.Members)
				: GetGossipCandidates(_settings.GossipSeeds);
			foreach (var candidate in gossipCandidates) {
				var gossip = await _gossipClient.GetAsync(candidate, cancellationToken).ConfigureAwait(false);

				if (gossip?.Members == null || gossip.Members.Length == 0)
					continue;

				if (!TryDetermineBestNode(out var bestNode)) {
					continue;
				}

				_oldGossip = gossip;

				return bestNode;

				bool TryDetermineBestNode(out EndPoint? endPoint) {
					var nodes = gossip.Members.Where(x => x.IsAlive)
						.Where(x => !_notAllowedStates.Contains(x.State))
						.OrderByDescending(x => x.State)
						.ToArray();

					switch (_settings.NodePreference) {
						case NodePreference.Random:
							nodes.RandomShuffle(0, nodes.Length - 1);
							break;
						case NodePreference.Leader:
							nodes = nodes.OrderBy(nodeEntry => nodeEntry.State != ClusterMessages.VNodeState.Leader)
								.ToArray();
							nodes.RandomShuffle(0,
								nodes.Count(nodeEntry => nodeEntry.State == ClusterMessages.VNodeState.Leader) - 1);
							break;
						case NodePreference.Follower:
							nodes = nodes.OrderBy(nodeEntry => nodeEntry.State != ClusterMessages.VNodeState.Follower)
								.ToArray();
							nodes.RandomShuffle(0,
								nodes.Count(nodeEntry => nodeEntry.State == ClusterMessages.VNodeState.Follower) - 1);
							break;
						case NodePreference.ReadOnlyReplica:
							nodes = nodes.OrderBy(IsNotReadOnlyReplica).ToArray();
							nodes.RandomShuffle(0,
								nodes.Count(nodeEntry => !IsNotReadOnlyReplica(nodeEntry)) - 1);
							break;
					}

					endPoint = nodes.FirstOrDefault()?.EndPoint;

					return endPoint != null;
				}
			}

			return null;
		}

		private static EndPoint[] GetGossipCandidates(EndPoint[] gossipSeeds) {
			var endpoints = new EndPoint[gossipSeeds.Length];
			Array.Copy(gossipSeeds, endpoints, gossipSeeds.Length);
			endpoints.RandomShuffle(0, endpoints.Length - 1);
			return endpoints;
		}

		private static EndPoint[] ArrangeGossipCandidates(IReadOnlyList<ClusterMessages.MemberInfo> members) {
			var result = new EndPoint[members.Count];
			int i = -1;
			int j = members.Count;
			for (int k = 0; k < members.Count; ++k) {
				if (members[k].State == ClusterMessages.VNodeState.Manager)
					result[--j] = members[k].EndPoint;
				else
					result[++i] = members[k].EndPoint;
			}

			result.RandomShuffle(0, i);
			result.RandomShuffle(j, members.Count - 1);

			return result;
		}

		private static bool IsNotReadOnlyReplica(ClusterMessages.MemberInfo node) =>
			!(node.State == ClusterMessages.VNodeState.ReadOnlyLeaderless ||
			  node.State == ClusterMessages.VNodeState.ReadOnlyReplica);
	}
}
