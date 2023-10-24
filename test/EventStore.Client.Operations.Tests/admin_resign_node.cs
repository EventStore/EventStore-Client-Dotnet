namespace EventStore.Client;

public class admin_resign_node : EventStoreFixture {
    public admin_resign_node(ITestOutputHelper output) : base(output) { }

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