namespace EventStore.Client.SubscriptionToStream; 

public class connect_to_existing_with_start_from_beginning_and_no_stream
    : IClassFixture<connect_to_existing_with_start_from_beginning_and_no_stream.Fixture> {
    private readonly Fixture _fixture;

    private const string Group = "startinbeginning1";

    private const string Stream =
        nameof(connect_to_existing_with_start_from_beginning_and_no_stream);

    public connect_to_existing_with_start_from_beginning_and_no_stream(Fixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public async Task the_subscription_gets_event_zero_as_its_first_event() {
        var firstEvent = await _fixture.FirstEvent.WithTimeout(TimeSpan.FromSeconds(10));
        Assert.Equal(StreamPosition.Start, firstEvent.Event.EventNumber);
        Assert.Equal(_fixture.EventId, firstEvent.Event.EventId);
    }

    public class Fixture : EventStoreClientFixture {
        private readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
        public           Task<ResolvedEvent>                 FirstEvent => _firstEventSource.Task;
        public           Uuid                                EventId    => Events.Single().EventId;
        public readonly  EventData[]                         Events;
        private          PersistentSubscription?             _subscription;

        public Fixture() {
            _firstEventSource = new TaskCompletionSource<ResolvedEvent>();
            Events            = CreateTestEvents().ToArray();
        }

        protected override async Task Given() {
            await Client.CreateToStreamAsync(Stream, Group,
                                             new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
            _subscription = await Client.SubscribeToStreamAsync(Stream, Group,
                                                                async (subscription, e, r, ct) => {
                                                                    _firstEventSource.TrySetResult(e);
                                                                    await subscription.Ack(e);
                                                                }, (subscription, reason, ex) => {
                                                                    if (reason != SubscriptionDroppedReason.Disposed) {
                                                                        _firstEventSource.TrySetException(ex!);
                                                                    }
                                                                }, TestCredentials.TestUser1);
        }

        protected override Task When()
            => StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}