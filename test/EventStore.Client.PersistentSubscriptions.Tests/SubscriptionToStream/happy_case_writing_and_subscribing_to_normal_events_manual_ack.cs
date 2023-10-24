namespace EventStore.Client.SubscriptionToStream;

public class happy_case_writing_and_subscribing_to_normal_events_manual_ack
    : IClassFixture<happy_case_writing_and_subscribing_to_normal_events_manual_ack.Fixture> {
    const string Stream          = nameof(happy_case_writing_and_subscribing_to_normal_events_manual_ack);
    const string Group           = nameof(Group);
    const int    BufferCount     = 10;
    const int    EventWriteCount = BufferCount * 2;

    readonly Fixture _fixture;

    public happy_case_writing_and_subscribing_to_normal_events_manual_ack(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Test() => await _fixture.EventsReceived.WithTimeout();

    public class Fixture : EventStoreClientFixture {
        readonly EventData[]                _events;
        readonly TaskCompletionSource<bool> _eventsReceived;
        int                                 _eventReceivedCount;

        PersistentSubscription? _subscription;

        public Fixture() {
            _events         = CreateTestEvents(EventWriteCount).ToArray();
            _eventsReceived = new();
        }

        public Task EventsReceived => _eventsReceived.Task;

        protected override async Task Given() {
            await Client.CreateToStreamAsync(
                Stream,
                Group,
                new(startFrom: StreamPosition.End, resolveLinkTos: true),
                userCredentials: TestCredentials.Root
            );

            _subscription = await Client.SubscribeToStreamAsync(
                Stream,
                Group,
                async (subscription, e, retryCount, ct) => {
                    await subscription.Ack(e);
                    if (Interlocked.Increment(ref _eventReceivedCount) == _events.Length)
                        _eventsReceived.TrySetResult(true);
                },
                (s, r, e) => {
                    if (e != null)
                        _eventsReceived.TrySetException(e);
                },
                bufferSize: BufferCount,
                userCredentials: TestCredentials.Root
            );
        }

        protected override async Task When() {
            foreach (var e in _events)
                await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, new[] { e });
        }

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}