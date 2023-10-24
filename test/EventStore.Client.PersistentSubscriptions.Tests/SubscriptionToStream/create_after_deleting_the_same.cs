namespace EventStore.Client.SubscriptionToStream;

public class create_after_deleting_the_same
    : IClassFixture<create_after_deleting_the_same.Fixture> {
    const    string  Stream = nameof(create_after_deleting_the_same);
    readonly Fixture _fixture;

    public create_after_deleting_the_same(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_completion_succeeds() =>
        await _fixture.Client.CreateToStreamAsync(
            Stream,
            "existing",
            new(),
            userCredentials: TestCredentials.Root
        );

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override async Task When() {
            await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents());
            await Client.CreateToStreamAsync(
                Stream,
                "existing",
                new(),
                userCredentials: TestCredentials.Root
            );

            await Client.DeleteToStreamAsync(
                Stream,
                "existing",
                userCredentials: TestCredentials.Root
            );
        }
    }
}