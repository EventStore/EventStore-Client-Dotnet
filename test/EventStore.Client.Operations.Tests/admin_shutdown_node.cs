namespace EventStore.Client;

public class admin_shutdown_node : IClassFixture<EventStoreClientsFixture> {
    public admin_shutdown_node(EventStoreClientsFixture fixture, ITestOutputHelper output) =>
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreClientsFixture Fixture { get; }

    [Fact]
    public async Task shutdown_does_not_throw() => await Fixture.Operations.ShutdownAsync(userCredentials: TestCredentials.Root);

    [Fact]
    public async Task shutdown_without_credentials_throws() => await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Operations.ShutdownAsync());
}