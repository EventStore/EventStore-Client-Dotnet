using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static EventStore.Client.ClusterMessages.VNodeState;

namespace EventStore.Client {
	public class NodePreferenceComparerTests {
		private static ClusterMessages.VNodeState RunTest(IComparer<ClusterMessages.VNodeState> sut,
			params ClusterMessages.VNodeState[] states) =>
			states
				.OrderBy(state => state, sut)
				.ThenBy(_ => Guid.NewGuid())
				.First();

		public static IEnumerable<object[]> LeaderTestCases() {
			yield return new object[] {Leader, Leader, Follower, Follower, ReadOnlyReplica};
			yield return new object[] {Follower, Follower, Follower, ReadOnlyReplica};
			yield return new object[] {ReadOnlyReplica, ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless};
			yield return new object[] {PreReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless};
			yield return new object[] {ReadOnlyLeaderless, ReadOnlyLeaderless, DiscoverLeader};
		}

		[Theory, MemberData(nameof(LeaderTestCases))]
		internal void LeaderTests(ClusterMessages.VNodeState expected, params ClusterMessages.VNodeState[] states) {
			var actual = RunTest(NodePreferenceComparers.Leader, states);

			Assert.Equal(expected, actual);
		}

		public static IEnumerable<object[]> FollowerTestCases() {
			yield return new object[] {Follower, Leader, Follower, Follower, ReadOnlyReplica};
			yield return new object[] {Leader, Leader, ReadOnlyReplica, ReadOnlyReplica};
			yield return new object[] {ReadOnlyReplica, ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless};
			yield return new object[] {PreReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless};
			yield return new object[] {ReadOnlyLeaderless, ReadOnlyLeaderless, DiscoverLeader};
		}

		[Theory, MemberData(nameof(FollowerTestCases))]
		internal void FollowerTests(ClusterMessages.VNodeState expected, params ClusterMessages.VNodeState[] states) {
			var actual = RunTest(NodePreferenceComparers.Follower, states);

			Assert.Equal(expected, actual);
		}

		public static IEnumerable<object[]> ReadOnlyReplicaTestCases() {
			yield return new object[] {ReadOnlyReplica, Leader, Follower, Follower, ReadOnlyReplica};
			yield return new object[] {PreReadOnlyReplica, Leader, Follower, Follower, PreReadOnlyReplica};
			yield return new object[] {ReadOnlyLeaderless, Leader, Follower, Follower, ReadOnlyLeaderless};
			yield return new object[] {Leader, Leader, Follower, Follower};
			yield return new object[] {Follower, DiscoverLeader, Follower, Follower};
		}

		[Theory, MemberData(nameof(ReadOnlyReplicaTestCases))]
		internal void ReadOnlyReplicaTests(ClusterMessages.VNodeState expected,
			params ClusterMessages.VNodeState[] states) {
			var actual = RunTest(NodePreferenceComparers.ReadOnlyReplica, states);

			Assert.Equal(expected, actual);
		}
	}
}
