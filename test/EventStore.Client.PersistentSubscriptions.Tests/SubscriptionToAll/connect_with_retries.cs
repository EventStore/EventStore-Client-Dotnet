namespace EventStore.Client.SubscriptionToAll;

public class connect_with_retries
    : IClassFixture<connect_with_retries.Fixture> {
    const    string  Group = "retries";
    readonly Fixture _fixture;

    public connect_with_retries(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task events_are_retried_until_success() => Assert.Equal(5, await _fixture.RetryCount.WithTimeout());

    public class Fixture : EventStoreClientFixture {
        readonly TaskCompletionSource<int> _retryCountSource;
        PersistentSubscription?            _subscription;

        public Fixture() => _retryCountSource = new();

        public Task<int> RetryCount => _retryCountSource.Task;

        protected override async Task Given() {
            await Client.CreateToAllAsync(
                Group,
                new(startFrom: Position.Start),
                userCredentials: TestCredentials.Root
            );

            _subscription = await Client.SubscribeToAllAsync(
                Group,
                async (subscription, e, r, ct) => {
                    if (r > 4) {
                        _retryCountSource.TrySetResult(r.Value);
                        await subscription.Ack(e.Event.EventId);
                    }
                    else {
                        await subscription.Nack(
                            PersistentSubscriptionNakEventAction.Retry,
                            "Not yet tried enough times",
                            e
                        );
                    }
                },
                (subscription, reason, ex) => {
                    if (reason != SubscriptionDroppedReason.Disposed)
                        _retryCountSource.TrySetException(ex!);
                },
                TestCredentials.Root
            );
        }

        protected override Task When() => Task.CompletedTask;

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}