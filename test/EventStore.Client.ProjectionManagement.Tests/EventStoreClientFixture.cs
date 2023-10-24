namespace EventStore.Client;

public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
    public EventStoreUserManagementClient       UserManagementClient { get; }
    public EventStoreClient                     StreamsClient        { get; }
    public EventStoreProjectionManagementClient Client               { get; }

    protected EventStoreClientFixture(EventStoreClientSettings? settings = null, bool noDefaultCredentials = false) :
        base(
            settings,
            new Dictionary<string, string> {
                ["EVENTSTORE_RUN_PROJECTIONS"]            = "ALL",
                ["EVENTSTORE_START_STANDARD_PROJECTIONS"] = "True"
            },
            noDefaultCredentials
        ) {
        Client               = new EventStoreProjectionManagementClient(Settings);
        UserManagementClient = new EventStoreUserManagementClient(Settings);
        StreamsClient        = new EventStoreClient(Settings);
    }

    protected virtual bool RunStandardProjections => true;

    protected override async Task OnServerUpAsync() {
        await StreamsClient.WarmUp();
        await UserManagementClient.WarmUp();
        await Client.WarmUp();
        await UserManagementClient.CreateUserWithRetry(
            TestCredentials.TestUser1.Username!,
            TestCredentials.TestUser1.Username!,
            Array.Empty<string>(),
            TestCredentials.TestUser1.Password!,
            TestCredentials.Root
        ).WithTimeout();

        await StandardProjections.Created(Client).WithTimeout(TimeSpan.FromMinutes(2));

        if (RunStandardProjections) {
            await Task
                .WhenAll(StandardProjections.Names.Select(name => Client.EnableAsync(name, userCredentials: TestCredentials.Root)))
                .WithTimeout(TimeSpan.FromMinutes(2));
        }
    }

    public override async Task DisposeAsync() {
        await StreamsClient.DisposeAsync();
        await UserManagementClient.DisposeAsync();
        await Client.DisposeAsync();
        await base.DisposeAsync();
    }
}