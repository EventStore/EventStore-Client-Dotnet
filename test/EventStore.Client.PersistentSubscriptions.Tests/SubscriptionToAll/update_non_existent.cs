namespace EventStore.Client.SubscriptionToAll; 

public class update_non_existent
    : IClassFixture<update_non_existent.Fixture> {

    private const    string  Group = "nonexistent";
    private readonly Fixture _fixture;

    public update_non_existent(Fixture fixture) {
        _fixture = fixture;
    }

    [SupportsPSToAll.Fact]
    public async Task the_completion_fails_with_not_found() {
        await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
            () => _fixture.Client.UpdateToAllAsync(Group,
                                                   new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root));
    }

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() => Task.CompletedTask;
        protected override Task When()  => Task.CompletedTask;
    }
}