namespace EventStore.Client.SubscriptionToAll;

public class create_without_permissions
    : IClassFixture<create_without_permissions.Fixture> {
    readonly Fixture _fixture;

    public create_without_permissions(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public Task the_completion_fails_with_access_denied() =>
        Assert.ThrowsAsync<AccessDeniedException>(
            () =>
                _fixture.Client.CreateToAllAsync(
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