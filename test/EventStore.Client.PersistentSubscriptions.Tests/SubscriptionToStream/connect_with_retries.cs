namespace EventStore.Client.SubscriptionToStream;

public class connect_with_retries
    : IClassFixture<connect_with_retries.Fixture> {
    const string Group  = "retries";
    const string Stream = nameof(connect_with_retries);

    readonly Fixture _fixture;

    public connect_with_retries(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task events_are_retried_until_success() => Assert.Equal(5, await _fixture.RetryCount.WithTimeout());

    public class Fixture : EventStoreClientFixture {
        readonly TaskCompletionSource<int> _retryCountSource;

        public readonly EventData[] Events;

        PersistentSubscription? _subscription;

        public Fixture() {
            _retryCountSource = new();

            Events = CreateTestEvents().ToArray();
        }

        public Task<int> RetryCount => _retryCountSource.Task;

        protected override async Task Given() {
            await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
            await Client.CreateToStreamAsync(
                Stream,
                Group,
                new(startFrom: StreamPosition.Start),
                userCredentials: TestCredentials.Root
            );

            _subscription = await Client.SubscribeToStreamAsync(
                Stream,
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
                TestCredentials.TestUser1
            );
        }

        protected override Task When() => StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}