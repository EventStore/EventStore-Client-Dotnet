namespace EventStore.Client.SubscriptionToAll;

public class update_existing_without_permissions
    : IClassFixture<update_existing_without_permissions.Fixture> {
    const    string  Group = "existing";
    readonly Fixture _fixture;

    public update_existing_without_permissions(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task the_completion_fails_with_access_denied() =>
        await Assert.ThrowsAsync<AccessDeniedException>(
            () => _fixture.Client.UpdateToAllAsync(
                Group,
                new()
            )
        );

    public class Fixture : EventStoreClientFixture {
        public Fixture() : base(noDefaultCredentials: true) { }

        protected override async Task Given() =>
            await Client.CreateToAllAsync(
                Group,
                new(),
                userCredentials: TestCredentials.Root
            );

        protected override Task When() => Task.CompletedTask;
    }
}