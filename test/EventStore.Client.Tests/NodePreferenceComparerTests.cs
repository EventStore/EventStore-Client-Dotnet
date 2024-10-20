using static EventStore.Client.ClusterMessages.VNodeState;

namespace EventStore.Client.Tests;

public class NodePreferenceComparerTests {
	static ClusterMessages.VNodeState RunTest(IComparer<ClusterMessages.VNodeState> sut, params ClusterMessages.VNodeState[] states) =>
		states
			.OrderBy(state => state, sut)
			.ThenBy(_ => Guid.NewGuid())
			.First();

	public static IEnumerable<object?[]> LeaderTestCases() {
		yield return [Leader, Leader, Follower, Follower, ReadOnlyReplica];
		yield return [Follower, Follower, Follower, ReadOnlyReplica];
		yield return [ReadOnlyReplica, ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless];
		yield return [PreReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless];
		yield return [ReadOnlyLeaderless, ReadOnlyLeaderless, DiscoverLeader];
	}

	[Theory]
	[MemberData(nameof(LeaderTestCases))]
	internal void LeaderTests(ClusterMessages.VNodeState expected, params ClusterMessages.VNodeState[] states) {
		var actual = RunTest(NodePreferenceComparers.Leader, states);

		Assert.Equal(expected, actual);
	}

	public static IEnumerable<object?[]> FollowerTestCases() {
		yield return [Follower, Leader, Follower, Follower, ReadOnlyReplica];
		yield return [Leader, Leader, ReadOnlyReplica, ReadOnlyReplica];
		yield return [ReadOnlyReplica, ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless];
		yield return [PreReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless];
		yield return [ReadOnlyLeaderless, ReadOnlyLeaderless, DiscoverLeader];
	}

	[Theory]
	[MemberData(nameof(FollowerTestCases))]
	internal void FollowerTests(ClusterMessages.VNodeState expected, params ClusterMessages.VNodeState[] states) {
		var actual = RunTest(NodePreferenceComparers.Follower, states);

		Assert.Equal(expected, actual);
	}

	public static IEnumerable<object?[]> ReadOnlyReplicaTestCases() {
		yield return [ReadOnlyReplica, Leader, Follower, Follower, ReadOnlyReplica];
		yield return [PreReadOnlyReplica, Leader, Follower, Follower, PreReadOnlyReplica];
		yield return [ReadOnlyLeaderless, Leader, Follower, Follower, ReadOnlyLeaderless];
		yield return [Leader, Leader, Follower, Follower];
		yield return [Follower, DiscoverLeader, Follower, Follower];
	}

	[Theory]
	[MemberData(nameof(ReadOnlyReplicaTestCases))]
	internal void ReadOnlyReplicaTests(ClusterMessages.VNodeState expected, params ClusterMessages.VNodeState[] states) {
		var actual = RunTest(NodePreferenceComparers.ReadOnlyReplica, states);

		Assert.Equal(expected, actual);
	}
}
