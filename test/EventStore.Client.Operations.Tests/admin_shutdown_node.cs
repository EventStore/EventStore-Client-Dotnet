namespace EventStore.Client;

public class admin_shutdown_node : EventStoreFixture {
    public admin_shutdown_node(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task shutdown_does_not_throw() => await Fixture.Operations.ShutdownAsync(userCredentials: TestCredentials.Root);

    [Fact]
    public async Task shutdown_without_credentials_throws() => await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Operations.ShutdownAsync());
}