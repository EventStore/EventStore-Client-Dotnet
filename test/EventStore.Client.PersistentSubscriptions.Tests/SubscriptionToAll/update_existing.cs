namespace EventStore.Client.SubscriptionToAll;

public class update_existing
    : IClassFixture<update_existing.Fixture> {
    const    string  Group = "existing";
    readonly Fixture _fixture;

    public update_existing(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task the_completion_succeeds() =>
        await _fixture.Client.UpdateToAllAsync(
            Group,
            new(),
            userCredentials: TestCredentials.Root
        );

    public class Fixture : EventStoreClientFixture {
        protected override async Task Given() =>
            await Client.CreateToAllAsync(
                Group,
                new(),
                userCredentials: TestCredentials.Root
            );

        protected override Task When() => Task.CompletedTask;
    }
}