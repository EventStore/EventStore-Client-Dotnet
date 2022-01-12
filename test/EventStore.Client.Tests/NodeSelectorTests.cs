﻿using System.Collections.Generic;
using System.Net;
using Xunit;

namespace EventStore.Client {
	public class NodeSelectorTests {
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
			ClusterMessages.VNodeState.DiscoverLeader
		};

		public static IEnumerable<object[]> InvalidStatesCases() {
			foreach (var state in _notAllowedStates) {
				var allowedNodeId = Uuid.NewUuid();
				var allowedNode = new DnsEndPoint(allowedNodeId.ToString(), 2113);

				var notAllowedNodeId = Uuid.NewUuid();
				var notAllowedNode = new DnsEndPoint(notAllowedNodeId.ToString(), 2114);

				var settings = new EventStoreClientSettings {
					ConnectivitySettings = {
						DnsGossipSeeds = new[] {allowedNode, notAllowedNode},
						Insecure = true
					}
				};

				yield return new object[] {
					new ClusterMessages.ClusterInfo(new ClusterMessages.MemberInfo[] {
						new(allowedNodeId, ClusterMessages.VNodeState.Leader, true, allowedNode),
						new(notAllowedNodeId, state, true, notAllowedNode),
					}),
					settings,
					allowedNode
				};
			}
		}

		[Theory, MemberData(nameof(InvalidStatesCases))]
		internal void InvalidStatesAreNotConsidered(
			ClusterMessages.ClusterInfo clusterInfo,
			EventStoreClientSettings settings,
			DnsEndPoint allowedNode) {

			var sut = new NodeSelector(settings);
			var selectedNode = sut.SelectNode(clusterInfo);

			Assert.Equal(allowedNode.Host, selectedNode.Host);
			Assert.Equal(allowedNode.Port, selectedNode.Port);
		}

		[Fact]
		public void DeadNodesAreNotConsidered() {
			var allowedNodeId = Uuid.NewUuid();
			var allowedNode = new DnsEndPoint(allowedNodeId.ToString(), 2113);

			var notAllowedNodeId = Uuid.NewUuid();
			var notAllowedNode = new DnsEndPoint(notAllowedNodeId.ToString(), 2114);

			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					DnsGossipSeeds = new[] {allowedNode, notAllowedNode},
					Insecure = true
				}
			};

			var sut = new NodeSelector(settings);
			var selectedNode = sut.SelectNode(new ClusterMessages.ClusterInfo(
				new ClusterMessages.MemberInfo[] {
					new(allowedNodeId, ClusterMessages.VNodeState.Follower, true, allowedNode),
					new(notAllowedNodeId, ClusterMessages.VNodeState.Leader, false, notAllowedNode),
				}));

			Assert.Equal(allowedNode.Host, selectedNode.Host);
			Assert.Equal(allowedNode.Port, selectedNode.Port);
		}

		[Theory]
		[InlineData(NodePreference.Leader, "leader")]
		[InlineData(NodePreference.Follower, "follower2")]
		[InlineData(NodePreference.ReadOnlyReplica, "readOnlyReplica")]
		[InlineData(NodePreference.Random, "any")]
		public void CanPrefer(NodePreference nodePreference, string expectedHost) {
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					NodePreference = nodePreference,
				}
			};

			var sut = new NodeSelector(settings);
			var selectedNode = sut.SelectNode(new ClusterMessages.ClusterInfo(
				new ClusterMessages.MemberInfo[] {
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Follower, false, new DnsEndPoint("follower1", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Leader, true, new DnsEndPoint("leader", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Follower, true, new DnsEndPoint("follower2", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.ReadOnlyReplica, true, new DnsEndPoint("readOnlyReplica", 2113)),
				}));

			if (expectedHost == "any")
				return;
			Assert.Equal(expectedHost, selectedNode.Host);
		}
	}
}
