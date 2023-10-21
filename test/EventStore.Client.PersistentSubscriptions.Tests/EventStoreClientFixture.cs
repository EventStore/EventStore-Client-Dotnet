namespace EventStore.Client; 

public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
    private readonly bool _skipPsWarmUp;

    public EventStorePersistentSubscriptionsClient Client               { get; }
    public EventStoreClient                        StreamsClient        { get; }
    public EventStoreUserManagementClient          UserManagementClient { get; }

    protected EventStoreClientFixture(EventStoreClientSettings? settings = null, bool skipPSWarmUp=false, bool noDefaultCredentials=false)
        : base(settings, noDefaultCredentials: noDefaultCredentials){

        _skipPsWarmUp = skipPSWarmUp;
			
        Client               = new EventStorePersistentSubscriptionsClient(Settings);
        StreamsClient        = new EventStoreClient(Settings);
        UserManagementClient = new EventStoreUserManagementClient(Settings);
    }

    protected override async Task OnServerUpAsync() {
        await StreamsClient.WarmUp();
        await UserManagementClient.WarmUp();
			
        if (!_skipPsWarmUp) {
            await Client.WarmUp();
        }

        await UserManagementClient.CreateUserWithRetry(TestCredentials.TestUser1.Username!,
                                                       TestCredentials.TestUser1.Username!, Array.Empty<string>(), TestCredentials.TestUser1.Password!,
                                                       TestCredentials.Root);
    }

    public override async Task DisposeAsync() {
        await UserManagementClient.DisposeAsync();
        await StreamsClient.DisposeAsync();
        await Client.DisposeAsync();
        await base.DisposeAsync();
    }
}