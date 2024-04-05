namespace EventStore.Client;

static class NodePreferenceComparers {
    public static readonly IComparer<ClusterMessages.VNodeState> Follower = new Comparer(
        state => state switch {
            ClusterMessages.VNodeState.Follower           => 0,
            ClusterMessages.VNodeState.Leader             => 1,
            ClusterMessages.VNodeState.ReadOnlyReplica    => 2,
            ClusterMessages.VNodeState.PreReadOnlyReplica => 3,
            ClusterMessages.VNodeState.ReadOnlyLeaderless => 4,
            _                                             => int.MaxValue
        }
    );

    public static readonly IComparer<ClusterMessages.VNodeState> Leader = new Comparer(
        state => state switch {
            ClusterMessages.VNodeState.Leader             => 0,
            ClusterMessages.VNodeState.Follower           => 1,
            ClusterMessages.VNodeState.ReadOnlyReplica    => 2,
            ClusterMessages.VNodeState.PreReadOnlyReplica => 3,
            ClusterMessages.VNodeState.ReadOnlyLeaderless => 4,
            _                                             => int.MaxValue
        }
    );

    public static readonly IComparer<ClusterMessages.VNodeState> None = new Comparer(_ => 0);

    public static readonly IComparer<ClusterMessages.VNodeState> ReadOnlyReplica = new Comparer(
        state => state switch {
            ClusterMessages.VNodeState.ReadOnlyReplica    => 0,
            ClusterMessages.VNodeState.PreReadOnlyReplica => 1,
            ClusterMessages.VNodeState.ReadOnlyLeaderless => 2,
            ClusterMessages.VNodeState.Leader             => 3,
            ClusterMessages.VNodeState.Follower           => 4,
            _                                             => int.MaxValue
        }
    );

    class Comparer(Func<ClusterMessages.VNodeState, int> getPriority) : IComparer<ClusterMessages.VNodeState> {
        public int Compare(ClusterMessages.VNodeState left, ClusterMessages.VNodeState right) =>
            getPriority(left).CompareTo(getPriority(right));
    }
}