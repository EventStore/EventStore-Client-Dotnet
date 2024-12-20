using System;
using System.Collections.Generic;

namespace EventStore.Client {
	internal static class NodePreferenceComparers {
		public static readonly IComparer<ClusterMessages.VNodeState> Leader = new Comparer(state =>
			state switch {
				ClusterMessages.VNodeState.Leader => 0,
				ClusterMessages.VNodeState.Follower => 1,
				ClusterMessages.VNodeState.ReadOnlyReplica => 2,
				ClusterMessages.VNodeState.PreReadOnlyReplica => 3,
				ClusterMessages.VNodeState.ReadOnlyLeaderless => 4,
				_ => int.MaxValue
			});

		public static readonly IComparer<ClusterMessages.VNodeState> Follower = new Comparer(state =>
			state switch {
				ClusterMessages.VNodeState.Follower => 0,
				ClusterMessages.VNodeState.Leader => 1,
				ClusterMessages.VNodeState.ReadOnlyReplica => 2,
				ClusterMessages.VNodeState.PreReadOnlyReplica => 3,
				ClusterMessages.VNodeState.ReadOnlyLeaderless => 4,
				_ => int.MaxValue
			});

		public static readonly IComparer<ClusterMessages.VNodeState> ReadOnlyReplica = new Comparer(state =>
			state switch {
				ClusterMessages.VNodeState.ReadOnlyReplica => 0,
				ClusterMessages.VNodeState.PreReadOnlyReplica => 1,
				ClusterMessages.VNodeState.ReadOnlyLeaderless => 2,
				ClusterMessages.VNodeState.Leader => 3,
				ClusterMessages.VNodeState.Follower => 4,
				_ => int.MaxValue
			});

		public static readonly IComparer<ClusterMessages.VNodeState> None = new Comparer(_ => 0);

		private class Comparer : IComparer<ClusterMessages.VNodeState> {
			private readonly Func<ClusterMessages.VNodeState, int> _getPriority;

			public Comparer(Func<ClusterMessages.VNodeState, int> getPriority) {
				_getPriority = getPriority;
			}

			public int Compare(ClusterMessages.VNodeState left, ClusterMessages.VNodeState right) =>
				_getPriority(left).CompareTo(_getPriority(right));
		}
	}
}
