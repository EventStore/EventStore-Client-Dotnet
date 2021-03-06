﻿using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	internal class ClusterMessages {
		public class ClusterInfo {
			public MemberInfo[]? Members { get; set; }
		}

		public class MemberInfo {
			public Guid InstanceId { get; set; }

			public VNodeState State { get; set; }
			public bool IsAlive { get; set; }
			public DnsEndPoint EndPoint { get; set; } = null!;
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
