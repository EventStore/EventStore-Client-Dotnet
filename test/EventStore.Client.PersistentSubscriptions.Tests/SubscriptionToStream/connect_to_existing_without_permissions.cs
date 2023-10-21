namespace EventStore.Client.SubscriptionToStream; 

public class connect_to_existing_without_permissions
    : IClassFixture<connect_to_existing_without_permissions.Fixture> {
    private const    string  Stream = "$" + nameof(connect_to_existing_without_permissions);
    private readonly Fixture _fixture;
    public connect_to_existing_without_permissions(Fixture fixture) { _fixture = fixture; }

    [Fact]
    public Task throws_access_denied() =>
        Assert.ThrowsAsync<AccessDeniedException>(async () => {
            using var _ = await _fixture.Client.SubscribeToStreamAsync(Stream, "agroupname55",
                                                                       delegate { return Task.CompletedTask; });
        }).WithTimeout();

    public class Fixture : EventStoreClientFixture {
        public Fixture () : base(noDefaultCredentials: true){
        }
			
        protected override Task Given() =>
            Client.CreateToStreamAsync(
                Stream,
                "agroupname55",
                new PersistentSubscriptionSettings(),
                userCredentials: TestCredentials.Root);

        protected override Task When() => Task.CompletedTask;
    }
}