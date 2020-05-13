using System;

#nullable enable
namespace EventStore.Client {
	public class ClusterMessages {
		public class ClusterInfo {
			public MemberInfo[]? Members { get; set; }
		}

		public class MemberInfo {
			public Guid InstanceId { get; set; }

			public VNodeState State { get; set; }
			public bool IsAlive { get; set; }
			public string? HttpEndPointIp { get; set; }
			public int HttpEndPointPort { get; set; }
		}

		public enum VNodeState {
			Initializing,
			Unknown,
			PreReplica,
			CatchingUp,
			Clone,
			Follower,
			PreLeader,
			Leader,
			Manager,
			ShuttingDown,
			Shutdown,
			ReadOnlyLeaderless,
			PreReadOnlyReplica,
			ReadOnlyReplica,
			ResigningLeader
		}
	}
}
