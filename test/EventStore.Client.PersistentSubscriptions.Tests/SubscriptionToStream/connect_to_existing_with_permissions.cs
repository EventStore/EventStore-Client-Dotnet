namespace EventStore.Client.SubscriptionToStream;

public class connect_to_existing_with_permissions
    : IClassFixture<connect_to_existing_with_permissions.Fixture> {
    const string Stream = nameof(connect_to_existing_with_permissions);

    readonly Fixture _fixture;

    public connect_to_existing_with_permissions(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_subscription_succeeds() {
        var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
        using var subscription = await _fixture.Client.SubscribeToStreamAsync(
            Stream,
            "agroupname17",
            delegate { return Task.CompletedTask; },
            (s, reason, ex) => dropped.TrySetResult((reason, ex)),
            TestCredentials.Root
        ).WithTimeout();

        Assert.NotNull(subscription);

        await Assert.ThrowsAsync<TimeoutException>(() => dropped.Task.WithTimeout());
    }

    public class Fixture : EventStoreClientFixture {
        protected override Task Given() =>
            Client.CreateToStreamAsync(
                Stream,
                "agroupname17",
                new(),
                userCredentials: TestCredentials.Root
            );

        protected override Task When() => Task.CompletedTask;
    }
}