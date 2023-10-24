namespace EventStore.Client.SubscriptionToStream;

public class deleting_nonexistent
    : IClassFixture<deleting_nonexistent.Fixture> {
    const    string  Stream = nameof(deleting_nonexistent);
    readonly Fixture _fixture;

    public deleting_nonexistent(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_delete_fails_with_argument_exception() =>
        await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
            () => _fixture.Client.DeleteToStreamAsync(
                Stream,
                Guid.NewGuid().ToString(),
                userCredentials: TestCredentials.Root
            )
        );

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;
        protected override Task When()  => Task.CompletedTask;
    }
}