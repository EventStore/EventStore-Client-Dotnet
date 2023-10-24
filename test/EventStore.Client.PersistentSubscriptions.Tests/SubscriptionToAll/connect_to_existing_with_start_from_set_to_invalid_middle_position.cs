namespace EventStore.Client.SubscriptionToAll;

public class connect_to_existing_with_start_from_set_to_invalid_middle_position
    : IClassFixture<connect_to_existing_with_start_from_set_to_invalid_middle_position.Fixture> {
    const    string  Group = "startfrominvalid1";
    readonly Fixture _fixture;

    public connect_to_existing_with_start_from_set_to_invalid_middle_position(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task the_subscription_is_dropped() {
        var (reason, exception) = await _fixture.Dropped.WithTimeout();
        Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
        Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
    }

    public class Fixture : EventStoreClientFixture {
        readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _dropped;

        PersistentSubscription? _subscription;

        public Fixture() => _dropped = new();

        public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _dropped.Task;

        protected override async Task Given() {
            var invalidPosition = new Position(1L, 1L);
            await Client.CreateToAllAsync(
                Group,
                new(startFrom: invalidPosition),
                userCredentials: TestCredentials.Root
            );
        }

        protected override async Task When() =>
            _subscription = await Client.SubscribeToAllAsync(
                Group,
                async (subscription, e, r, ct) => await subscription.Ack(e),
                (subscription, reason, ex) => { _dropped.TrySetResult((reason, ex)); },
                TestCredentials.Root
            );

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}