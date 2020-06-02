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
			Initializing = 0,
			DiscoverLeader = 1,
			Unknown = 2,
			PreReplica = 3,
			CatchingUp = 4,
			Clone = 5,
			Follower = 6,
			PreLeader = 7,
			Leader = 8,
			Manager = 9,
			ShuttingDown = 10,
			Shutdown = 11,
			ReadOnlyLeaderless = 12,
			PreReadOnlyReplica = 13,
			ReadOnlyReplica = 14,
			ResigningLeader = 15
		}
	}
}
