using System.Net;

namespace EventStore.Client;

// Selects a node to connect to from a ClusterInfo, based on the node preference.
// Deals with endpoints, no grpc here.
// Thread safe.
class NodeSelector(EventStoreClientSettings settings) {
	static readonly ClusterMessages.VNodeState[] NotAllowedStates = {
		ClusterMessages.VNodeState.Manager,
		ClusterMessages.VNodeState.ShuttingDown,
		ClusterMessages.VNodeState.Shutdown,
		ClusterMessages.VNodeState.Unknown,
		ClusterMessages.VNodeState.Initializing,
		ClusterMessages.VNodeState.CatchingUp,
		ClusterMessages.VNodeState.ResigningLeader,
		ClusterMessages.VNodeState.PreLeader,
		ClusterMessages.VNodeState.PreReplica,
		ClusterMessages.VNodeState.PreReadOnlyReplica,
		ClusterMessages.VNodeState.Clone,
		ClusterMessages.VNodeState.DiscoverLeader
	};

	readonly IComparer<ClusterMessages.VNodeState>? _nodeStateComparer = settings.ConnectivitySettings.NodePreference switch {
		NodePreference.Leader          => NodePreferenceComparers.Leader,
		NodePreference.Follower        => NodePreferenceComparers.Follower,
		NodePreference.ReadOnlyReplica => NodePreferenceComparers.ReadOnlyReplica,
		_                              => NodePreferenceComparers.None
	};

	readonly Random _random = new(0);

	public DnsEndPoint SelectNode(ClusterMessages.ClusterInfo clusterInfo) {
		if (clusterInfo.Members.Length == 0) throw new Exception("No nodes in cluster info.");

		lock (_random) {
			var node = clusterInfo.Members
				.Where(IsConnectable)
				.OrderBy(node => node.State, _nodeStateComparer)
				.ThenBy(_ => _random.Next())
				.FirstOrDefault();

			if (node is null) throw new Exception("No nodes are in a connectable state.");

			return node.EndPoint;
		}
	}

	static bool IsConnectable(ClusterMessages.MemberInfo node) =>
		node.IsAlive && !NotAllowedStates.Contains(node.State);
}