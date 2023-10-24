namespace EventStore.Client.SubscriptionToAll;

public class when_writing_and_filtering_out_events
    : IClassFixture<when_writing_and_filtering_out_events.Fixture> {
    const    string  Group = "filtering-out-events";
    readonly Fixture _fixture;

    public when_writing_and_filtering_out_events(Fixture fixture) => _fixture = fixture;

    [SupportsPSToAll.Fact]
    public async Task it_should_write_a_check_point() {
        await _fixture.SecondCheckPoint.WithTimeout();
        var secondCheckPoint = _fixture.SecondCheckPoint.Result.Event.Data.ParsePosition();
        Assert.True(secondCheckPoint > _fixture.FirstCheckPoint);
        Assert.Equal(
            _fixture.Events.Select(e => e.EventId),
            _fixture.AppearedEvents.Select(e => e.Event.EventId)
        );
    }

    public class Fixture : EventStoreClientFixture {
        readonly TaskCompletionSource<bool> _appeared;
        readonly List<ResolvedEvent>        _appearedEvents, _checkPoints;
        readonly string                     _checkPointStream = $"$persistentsubscription-$all::{Group}-checkpoint";
        readonly EventData[]                _events;

        readonly TaskCompletionSource<ResolvedEvent> _firstCheckPointSource, _secondCheckPointSource;
        StreamSubscription?                          _checkPointSubscription;
        PersistentSubscription?                      _subscription;

        public Fixture() {
            _firstCheckPointSource  = new();
            _secondCheckPointSource = new();
            _appeared               = new();
            _appearedEvents         = new();
            _checkPoints            = new();
            _events                 = CreateTestEvents(5).ToArray();
        }

        public Task<ResolvedEvent> SecondCheckPoint => _secondCheckPointSource.Task;
        public Position            FirstCheckPoint  { get; private set; }
        public EventData[]         Events           => _events.ToArray();
        public ResolvedEvent[]     AppearedEvents   => _appearedEvents.ToArray();

        protected override async Task Given() {
            foreach (var e in _events)
                await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });

            await Client.CreateToAllAsync(
                Group,
                StreamFilter.Prefix("test"),
                new(
                    checkPointLowerBound: 5,
                    checkPointAfter: TimeSpan.FromSeconds(1),
                    startFrom: Position.Start
                ),
                userCredentials: TestCredentials.Root
            );

            _checkPointSubscription = await StreamsClient.SubscribeToStreamAsync(
                _checkPointStream,
                FromStream.Start,
                (_, e, _) => {
                    if (_checkPoints.Count == 0)
                        _firstCheckPointSource.TrySetResult(e);
                    else
                        _secondCheckPointSource.TrySetResult(e);

                    _checkPoints.Add(e);
                    return Task.CompletedTask;
                },
                userCredentials: TestCredentials.Root
            );

            _subscription = await Client.SubscribeToAllAsync(
                Group,
                async (s, e, r, ct) => {
                    _appearedEvents.Add(e);

                    if (_appearedEvents.Count == _events.Length)
                        _appeared.TrySetResult(true);

                    await s.Ack(e);
                },
                userCredentials: TestCredentials.Root
            );

            await Task.WhenAll(_appeared.Task, _firstCheckPointSource.Task).WithTimeout();

            FirstCheckPoint = _firstCheckPointSource.Task.Result.Event.Data.ParsePosition();
        }

        protected override async Task When() {
            foreach (var e in _events)
                await StreamsClient.AppendToStreamAsync(
                    "filtered-out-stream-" + Guid.NewGuid(),
                    StreamState.Any,
                    new[] { e }
                );
        }

        public override Task DisposeAsync() {
            _subscription?.Dispose();
            _checkPointSubscription?.Dispose();
            return base.DisposeAsync();
        }
    }
}