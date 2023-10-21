namespace EventStore.Client; 

public class @admin : IClassFixture<EventStoreClientsFixture> {
    public admin(EventStoreClientsFixture fixture, ITestOutputHelper output) =>
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreClientsFixture Fixture { get; }
    
    [Fact]
    public async Task merge_indexes_does_not_throw() {
        await Fixture.Operations
            .MergeIndexesAsync(userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();
    }

    [Fact]
    public async Task merge_indexes_without_credentials_throws() {
        await Fixture.Operations
            .MergeIndexesAsync()
            .ShouldThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task restart_persistent_subscriptions_does_not_throw() =>
        await Fixture.Operations
            .RestartPersistentSubscriptions(userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();

    [Fact]
    public async Task restart_persistent_subscriptions_without_credentials_throws() {
        await Fixture.Operations
            .RestartPersistentSubscriptions()
            .ShouldThrowAsync<AccessDeniedException>();
    }
}