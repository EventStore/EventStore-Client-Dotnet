namespace EventStore.Client.SubscriptionToAll;

public class update_existing_with_commit_position_equal_to_last_indexed_position
    : IClassFixture<update_existing_with_commit_position_equal_to_last_indexed_position.Fixture> {
    const string Group = "existing";

    readonly Fixture _fixture;

    public update_existing_with_commit_position_equal_to_last_indexed_position(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task the_completion_succeeds() =>
        await _fixture.Client.UpdateToAllAsync(
            Group,
            new(startFrom: new Position(_fixture.LastCommitPosition, _fixture.LastCommitPosition)),
            userCredentials: TestCredentials.Root
        );

    public class Fixture : EventStoreClientFixture {
        public ulong LastCommitPosition;

        protected override async Task Given() {
            await Client.CreateToAllAsync(
                Group,
                new(),
                userCredentials: TestCredentials.Root
            );

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