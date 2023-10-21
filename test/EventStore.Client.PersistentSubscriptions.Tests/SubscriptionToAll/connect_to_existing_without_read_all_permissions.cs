namespace EventStore.Client.SubscriptionToAll; 

public class connect_to_existing_without_read_all_permissions
    : IClassFixture<connect_to_existing_without_read_all_permissions.Fixture> {

    private readonly Fixture _fixture;
    public connect_to_existing_without_read_all_permissions(Fixture fixture) { _fixture = fixture; }

    [SupportsPSToAll.Fact]
    public Task throws_access_denied() =>
        Assert.ThrowsAsync<AccessDeniedException>(async () => {
            using var _ = await _fixture.Client.SubscribeToAllAsync("agroupname55",
                                                                    delegate { return Task.CompletedTask; }, userCredentials: TestCredentials.TestUser1);
        }).WithTimeout();

    public class Fixture : EventStoreClientFixture {
        public Fixture () : base(noDefaultCredentials: true){
        }
			
        protected override Task Given() =>
            Client.CreateToAllAsync(
                "agroupname55",
                new PersistentSubscriptionSettings(),
                userCredentials: TestCredentials.Root);

        protected override Task When() => Task.CompletedTask;
    }
}