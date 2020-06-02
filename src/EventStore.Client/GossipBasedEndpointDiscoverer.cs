using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using EndPoint = System.Net.EndPoint;
using GossipClient = EventStore.Client.Gossip.Gossip.GossipClient;

#nullable enable
namespace EventStore.Client {
	public class ClusterEndpointDiscoverer : IEndpointDiscoverer {
		private readonly int _maxDiscoverAttempts;
		private readonly EndPoint[] _gossipSeeds;
		private readonly TimeSpan _discoveryInterval;
		private ClusterMessages.MemberInfo[]? _oldGossip;
		private readonly NodePreference _nodePreference;
		private readonly Dictionary<EndPoint, GossipClient> _gossipClients;
		private readonly Func<EndPoint, GossipClient> _gossipClientFactory;

		public ClusterEndpointDiscoverer(
			int maxDiscoverAttempts,
			EndPoint[] gossipSeeds,
			TimeSpan gossipTimeout,
			TimeSpan discoveryInterval,
			NodePreference nodePreference,
			HttpMessageHandler? httpMessageHandler = null) {
			_maxDiscoverAttempts = maxDiscoverAttempts;
			_gossipSeeds = gossipSeeds;
			_discoveryInterval = discoveryInterval;
			_nodePreference = nodePreference;
			_gossipClients = new Dictionary<EndPoint,  GossipClient>();
			_gossipClientFactory = (gossipSeedEndPoint) => {
				string url = gossipSeedEndPoint.ToHttpUrl(EndPointExtensions.HTTPS_SCHEMA);
				var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions {
					HttpClient = new HttpClient(httpMessageHandler ?? new HttpClientHandler()) {
						Timeout = gossipTimeout,
						DefaultRequestVersion = new Version(2, 0),
					}
				});
				var callInvoker = channel.CreateCallInvoker();
				return new GossipClient(callInvoker);
			};
		}

		public async Task<EndPoint> DiscoverAsync() {
			for (int attempt = 1; attempt <= _maxDiscoverAttempts; ++attempt) {
				try {
					var endpoint = await DiscoverEndpointAsync().ConfigureAwait(false);
					if (endpoint != null) {
						return endpoint;
					}
				} catch (Exception) {
				}

				await Task.Delay(_discoveryInterval).ConfigureAwait(false);
			}

			throw new DiscoveryException($"Failed to discover candidate in {_maxDiscoverAttempts} attempts.");
		}

		private async Task<EndPoint?> DiscoverEndpointAsync() {
			var oldGossip = Interlocked.Exchange(ref _oldGossip, null);
			var gossipCandidates = oldGossip != null
				? ArrangeGossipCandidates(oldGossip.ToArray())
				: GetGossipCandidates();
			foreach (var candidate in gossipCandidates) {
				var gossip = await TryGetGossipFrom(candidate).ConfigureAwait(false);
				if (gossip?.Members == null || gossip.Members.Length == 0)
					continue;

				var bestNode = TryDetermineBestNode(gossip.Members, _nodePreference);
				if (bestNode == null) continue;
				_oldGossip = gossip.Members;
				return bestNode;
			}

			return null;
		}

		private EndPoint[] GetGossipCandidates() {
			EndPoint[] endpoints = _gossipSeeds;
			RandomShuffle(endpoints, 0, endpoints.Length - 1);
			return endpoints;
		}

		private EndPoint[] ArrangeGossipCandidates(ClusterMessages.MemberInfo[] members) {
			var result = new EndPoint[members.Length];
			int i = -1;
			int j = members.Length;
			for (int k = 0; k < members.Length; ++k) {
				if (members[k].State == ClusterMessages.VNodeState.Manager)
					result[--j] = new DnsEndPoint(members[k].HttpEndPointIp,
						members[k].HttpEndPointPort);
				else
					result[++i] = new DnsEndPoint(members[k].HttpEndPointIp,
						members[k].HttpEndPointPort);
			}

			RandomShuffle(result, 0, i);
			RandomShuffle(result, j, members.Length - 1);

			return result;
		}

		private void RandomShuffle<T>(T[] arr, int i, int j) {
			if (i >= j)
				return;
			var rnd = new Random(Guid.NewGuid().GetHashCode());
			for (int k = i; k <= j; ++k) {
				var index = rnd.Next(k, j + 1);
				var tmp = arr[index];
				arr[index] = arr[k];
				arr[k] = tmp;
			}
		}

		private async Task<ClusterMessages.ClusterInfo> TryGetGossipFrom(EndPoint gossipSeed) {
			if (!_gossipClients.TryGetValue(gossipSeed, out var client)) {
				client = _gossipClientFactory(gossipSeed);
				_gossipClients[gossipSeed] = client;
			}

			var clusterInfoDto = await client.ReadAsync(new Empty());
			return ConvertGrpcClusterInfo(clusterInfoDto);
		}

		private static ClusterMessages.ClusterInfo ConvertGrpcClusterInfo(Gossip.ClusterInfo clusterInfo) {
			var receivedMembers = Array.ConvertAll(clusterInfo.Members.ToArray(), x =>
				new ClusterMessages.MemberInfo {
					InstanceId = Uuid.FromDto(x.InstanceId).ToGuid(),
					State = (ClusterMessages.VNodeState) x.State,
					IsAlive = x.IsAlive,
					HttpEndPointIp = x.HttpEndPoint.Address,
					HttpEndPointPort = (int)x.HttpEndPoint.Port
				});
			return new ClusterMessages.ClusterInfo { Members = receivedMembers };
		}

		private EndPoint? TryDetermineBestNode(IEnumerable<ClusterMessages.MemberInfo> members,
			NodePreference nodePreference) {
			var notAllowedStates = new[] {
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

			var nodes = members.Where(x => x.IsAlive)
				.Where(x => !notAllowedStates.Contains(x.State))
				.OrderByDescending(x => x.State)
				.ToArray();

			switch (nodePreference) {
				case NodePreference.Random:
					RandomShuffle(nodes, 0, nodes.Length - 1);
					break;
				case NodePreference.Leader:
					nodes = nodes.OrderBy(nodeEntry => nodeEntry.State != ClusterMessages.VNodeState.Leader)
						.ToArray();
					RandomShuffle(nodes, 0,
						nodes.Count(nodeEntry => nodeEntry.State == ClusterMessages.VNodeState.Leader) - 1);
					break;
				case NodePreference.Follower:
					nodes = nodes.OrderBy(nodeEntry => nodeEntry.State != ClusterMessages.VNodeState.Follower)
						.ToArray();
					RandomShuffle(nodes, 0,
						nodes.Count(nodeEntry => nodeEntry.State == ClusterMessages.VNodeState.Follower) - 1);
					break;
				case NodePreference.ReadOnlyReplica:
					nodes = nodes.OrderBy(nodeEntry => !IsReadOnlyReplicaState(nodeEntry.State))
						.ToArray();
					RandomShuffle(nodes, 0,
						nodes.Count(nodeEntry => IsReadOnlyReplicaState(nodeEntry.State)) - 1);
					break;
			}

			var node = nodes.FirstOrDefault();

			return node == default(ClusterMessages.MemberInfo)
				? null
				: new DnsEndPoint(node.HttpEndPointIp, node.HttpEndPointPort);
		}

		private bool IsReadOnlyReplicaState(ClusterMessages.VNodeState state) {
			return state == ClusterMessages.VNodeState.ReadOnlyLeaderless
			       || state == ClusterMessages.VNodeState.ReadOnlyReplica;
		}
	}
}
