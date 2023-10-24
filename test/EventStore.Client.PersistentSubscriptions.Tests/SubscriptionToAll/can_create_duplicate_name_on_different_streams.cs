namespace EventStore.Client.SubscriptionToAll;

public class can_create_duplicate_name_on_different_streams
    : IClassFixture<can_create_duplicate_name_on_different_streams.Fixture> {
    readonly Fixture _fixture;

    public can_create_duplicate_name_on_different_streams(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public Task the_completion_succeeds() =>
        _fixture.Client.CreateToStreamAsync(
            "someother",
            "group3211",
            new(),
            userCredentials: TestCredentials.Root
        );

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override Task When() =>
            Client.CreateToAllAsync(
                "group3211",
                new(),
                userCredentials: TestCredentials.Root
            );
    }
}