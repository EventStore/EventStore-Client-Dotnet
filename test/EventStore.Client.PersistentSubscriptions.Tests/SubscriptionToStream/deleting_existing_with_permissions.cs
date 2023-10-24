namespace EventStore.Client.SubscriptionToStream;

public class deleting_existing_with_permissions
    : IClassFixture<deleting_existing_with_permissions.Fixture> {
    const    string  Stream = nameof(deleting_existing_with_permissions);
    readonly Fixture _fixture;

    public deleting_existing_with_permissions(Fixture fixture) => _fixture = fixture;

    [Fact]
    public Task the_delete_of_group_succeeds() =>
        _fixture.Client.DeleteToStreamAsync(
            Stream,
            "groupname123",
            userCredentials: TestCredentials.Root
        );

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;

        protected override Task When() =>
            Client.CreateToStreamAsync(
                Stream,
                "groupname123",
                new(),
                userCredentials: TestCredentials.Root
            );
    }
}