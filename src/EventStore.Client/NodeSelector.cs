using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace EventStore.Client {
	// Selects a node to connect to from a ClusterInfo, based on the node preference.
	// Deals with endpoints, no grpc here.
	// Thread safe.
	internal class NodeSelector {
		private static readonly ClusterMessages.VNodeState[] _notAllowedStates = {
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
			ClusterMessages.VNodeState.DiscoverLeader,
		};

		private readonly Random _random;
		private readonly IComparer<ClusterMessages.VNodeState>? _nodeStateComparer;

		public NodeSelector(EventStoreClientSettings settings) {
			_random = new Random(0);
			_nodeStateComparer = settings.ConnectivitySettings.NodePreference switch {
				NodePreference.Leader => NodePreferenceComparers.Leader,
				NodePreference.Follower => NodePreferenceComparers.Follower,
				NodePreference.ReadOnlyReplica => NodePreferenceComparers.ReadOnlyReplica,
				_ => NodePreferenceComparers.None
			};
		}

		public ChannelIdentifier SelectNode(
			ClusterMessages.ClusterInfo clusterInfo, UserCredentials? userCredentials = null
		) {
			if (clusterInfo.Members.Length == 0) {
				throw new Exception("No nodes in cluster info.");
			}

			lock (_random) {
				var node = clusterInfo.Members
					.Where(IsConnectable)
					.OrderBy(node => node.State, _nodeStateComparer)
					.ThenBy(_ => _random.Next())
					.FirstOrDefault();

				if (node is null) {
					throw new Exception("No nodes are in a connectable state.");
				}

				return new ChannelIdentifier(node.EndPoint, userCredentials);
			}
		}

		private static bool IsConnectable(ClusterMessages.MemberInfo node) =>
			node.IsAlive &&
			!_notAllowedStates.Contains(node.State);
	}
}
