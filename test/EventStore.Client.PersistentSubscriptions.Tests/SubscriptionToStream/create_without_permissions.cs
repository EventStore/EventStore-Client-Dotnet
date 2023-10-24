namespace EventStore.Client.SubscriptionToStream;

public class create_without_permissions
    : IClassFixture<create_without_permissions.Fixture> {
    const    string  Stream = nameof(create_without_permissions);
    readonly Fixture _fixture;

    public create_without_permissions(Fixture fixture) => _fixture = fixture;

    [Fact]
    public Task the_completion_fails_with_access_denied() =>
        Assert.ThrowsAsync<AccessDeniedException>(
            () =>
                _fixture.Client.CreateToStreamAsync(
                    Stream,
                    "group57",
                    new()
                )
        );

    public class Fixture : EventStoreClientFixture {
        public Fixture() : base(noDefaultCredentials: true) { }

        protected override Task Given() => Task.CompletedTask;
        protected override Task When()  => Task.CompletedTask;
    }
}