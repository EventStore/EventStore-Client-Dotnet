namespace EventStore.Client;

public class admin_resign_node : IClassFixture<EventStoreClientsFixture> {
    public admin_resign_node(EventStoreClientsFixture fixture, ITestOutputHelper output) => 
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreClientsFixture Fixture { get; }

    [Fact]
    public async Task resign_node_does_not_throw() {
        await Fixture.Operations
            .ResignNodeAsync(userCredentials: TestCredentials.Root)
            .ShouldNotThrowAsync();
    }

    [Fact]
    public async Task resign_node_without_credentials_throws() {
        await Fixture.Operations
            .ResignNodeAsync()
            .ShouldThrowAsync<AccessDeniedException>();
    }
}