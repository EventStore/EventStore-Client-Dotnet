namespace EventStore.Client.SubscriptionToStream;

public class connect_to_existing_with_start_from_set_to_end_position_and_events_in_it
    : IClassFixture<connect_to_existing_with_start_from_set_to_end_position_and_events_in_it.Fixture> {
    const string Group = "startinbeginning1";

    const string Stream =
        nameof(connect_to_existing_with_start_from_set_to_end_position_and_events_in_it);

    readonly Fixture _fixture;

    public connect_to_existing_with_start_from_set_to_end_position_and_events_in_it(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task the_subscription_gets_no_events() => await Assert.ThrowsAsync<TimeoutException>(() => _fixture.FirstEvent.WithTimeout());

    public class Fixture : EventStoreClientFixture {
        readonly        TaskCompletionSource<ResolvedEvent> _firstEventSource;
        public readonly EventData[]                         Events;
        PersistentSubscription?                             _subscription;

        public Fixture() {
            _firstEventSource = new();
            Events            = CreateTestEvents(10).ToArray();
        }

        public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;

        protected override async Task Given() {
            await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
            await Client.CreateToStreamAsync(
                Stream,
                Group,
                new(startFrom: StreamPosition.End),
                userCredentials: TestCredentials.Root
            );
        }

        protected override async Task When() =>
            _subscription = await Client.SubscribeToStreamAsync(
                Stream,
                Group,
                async (subscription, e, r, ct) => {
                    _firstEventSource.TrySetResult(e);
                    await subscription.Ack(e);
                },
                (subscription, reason, ex) => {
                    if (reason != SubscriptionDroppedReason.Disposed)
                        _firstEventSource.TrySetException(ex!);
                },
                TestCredentials.TestUser1
            );

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}