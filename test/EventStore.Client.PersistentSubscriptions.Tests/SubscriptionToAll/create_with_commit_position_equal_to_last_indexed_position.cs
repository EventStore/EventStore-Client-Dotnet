namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class create_with_commit_position_equal_to_last_indexed_position : IClassFixture<create_with_commit_position_equal_to_last_indexed_position.Fixture> {
	readonly Fixture _fixture;

	public create_with_commit_position_equal_to_last_indexed_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_completion_succeeds() =>
		await _fixture.Client.CreateToAllAsync(
			"group57",
			new(startFrom: new Position(_fixture.LastCommitPosition, _fixture.LastCommitPosition)),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		public ulong LastCommitPosition;

		protected override async Task Given() {
			var lastEvent = await StreamsClient.ReadAllAsync(
				Direction.Backwards,
				Position.End,
				1,
				userCredentials: TestCredentials.Root
			).FirstAsync();

			LastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
		}

		protected override Task When() => Task.CompletedTask;
	}
}