namespace EventStore.Client.SubscriptionToStream;

public class deleting_without_permissions
    : IClassFixture<deleting_without_permissions.Fixture> {
    const    string  Stream = nameof(deleting_without_permissions);
    readonly Fixture _fixture;

    public deleting_without_permissions(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_delete_fails_with_access_denied() =>
        await Assert.ThrowsAsync<AccessDeniedException>(
            () => _fixture.Client.DeleteToStreamAsync(
                Stream,
                Guid.NewGuid().ToString()
            )
        );

    public class Fixture : EventStoreClientFixture {
        public Fixture() : base(noDefaultCredentials: true) { }

        protected override Task Given() => Task.CompletedTask;
        protected override Task When()  => Task.CompletedTask;
    }
}