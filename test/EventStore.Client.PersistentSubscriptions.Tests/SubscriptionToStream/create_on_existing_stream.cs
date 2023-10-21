namespace EventStore.Client.SubscriptionToStream; 

public class create_on_existing_stream
    : IClassFixture<create_on_existing_stream.Fixture> {
    public create_on_existing_stream(Fixture fixture) {
        _fixture = fixture;
    }

    private const    string  Stream = nameof(create_on_existing_stream);
    private readonly Fixture _fixture;

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override async Task When() =>
            await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents());
    }

    [Fact]
    public Task the_completion_succeeds()
        => _fixture.Client.CreateToStreamAsync(
            Stream, "existing", new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
}