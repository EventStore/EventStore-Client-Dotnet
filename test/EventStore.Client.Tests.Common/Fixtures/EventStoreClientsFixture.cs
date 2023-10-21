using System.Runtime.CompilerServices;
using System.Text;

namespace EventStore.Client.Tests;

public class EventStoreClientsFixture : EventStoreIntegrationFixture {
    const string TestEventType = "-";

    public EventStoreClient                        Client                  { get; private set; } = null!;
    public EventStoreClient                        Streams                 { get; private set; } = null!;
    public EventStoreOperationsClient              Operations              { get; private set; } = null!;
    public EventStoreUserManagementClient          Users                   { get; private set; } = null!;
    public EventStoreProjectionManagementClient    Projections             { get; private set; } = null!;
    public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; private set; } = null!;


    static readonly InterlockedBoolean WarmUpCompleted = new InterlockedBoolean();
    
    protected override async Task OnInitialized() {
        Client                  = new(Options.ClientSettings);
        Streams                 = Client;
        Operations              = new(Options.ClientSettings);
        Users                   = new(Options.ClientSettings);
        Projections             = new(Options.ClientSettings);
        PersistentSubscriptions = new(Options.ClientSettings);

        if (WarmUpCompleted.EnsureCalledOnce()) {
            try {
                await Task.WhenAll(
                    Streams.WarmUp(),
                    Operations.WarmUp(),
                    Users.WarmUp(),
                    Projections.WarmUp(),
                    PersistentSubscriptions.WarmUp()
                );
            }

            catch (Exception) {
                // ignored
            }
        }
        
        //TODO SS: in order to migrate/refactor code faster will keep Given() and When() apis for now
        await Given().WithTimeout(TimeSpan.FromMinutes(5));
        await When().WithTimeout(TimeSpan.FromMinutes(5));
    }
    
    protected virtual Task Given() => Task.CompletedTask;
    protected virtual Task When()  => Task.CompletedTask;

    public string GetStreamName([CallerMemberName] string? testMethod = null) => 
        $"{GetType().DeclaringType?.Name}.{testMethod ?? "unknown"}";

    public IEnumerable<EventData> CreateTestEvents(int count = 1, string? type = null, int metadataSize = 1) =>
        Enumerable.Range(0, count).Select(index => CreateTestEvent(index, type ?? TestEventType, metadataSize));

    protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType, 1);

    protected static EventData CreateTestEvent(int index, string type, int metadataSize) =>
        new(
            Uuid.NewUuid(),
            type,
            Encoding.UTF8.GetBytes($$"""{"x":{{index}}}"""),
            Encoding.UTF8.GetBytes($"\"{new string('$', metadataSize)}\"")
        );
    
    public async Task<TestUser> CreateTestUser(bool withoutGroups = true, bool useUserCredentials = false) {
        var result = await CreateTestUsers(1, withoutGroups, useUserCredentials);
        return result.First();
    }

    public Task<TestUser[]> CreateTestUsers(int count = 3, bool withoutGroups = true, bool useUserCredentials = false) =>
        Fakers.Users
            .RuleFor(x => x.Groups, f => withoutGroups ? Array.Empty<string>() : f.Lorem.Words()) 
            .Generate(count)
            .Select(
                async user => {
                    await Users.CreateUserAsync(
                        user.LoginName, user.FullName, user.Groups, user.Password,
                        userCredentials: useUserCredentials ? user.Credentials : TestCredentials.Root
                    );
    
                    return user;
                }
            ).WhenAll();
}

public class EventStoreInsecureClientsFixture : EventStoreClientsFixture {
    protected override EventStoreTestServiceOptions Override(EventStoreTestServiceOptions options) {
        options.ClientSettings.DefaultCredentials = null;
        return options;
    }
}