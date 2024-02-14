namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

[Obsolete]
public class when_writing_and_filtering_out_events_obsolete
	: IClassFixture<when_writing_and_filtering_out_events_obsolete.Fixture> {
	const string Group = "filtering-out-events";

	readonly Fixture _fixture;

	public when_writing_and_filtering_out_events_obsolete(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task it_should_write_a_check_point() {
		await Task.Yield();
		Assert.True(_fixture.SecondCheckPoint > _fixture.FirstCheckPoint);
		Assert.Equal(
			_fixture.Events.Select(e => e.EventId),
			_fixture.AppearedEvents.Select(e => e.Event.EventId)
		);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<bool> _appeared;
		readonly List<ResolvedEvent>        _appearedEvents, _checkPoints;
		readonly EventData[]                _events;

		PersistentSubscription?                      _subscription;

		public Fixture() {
			_appeared               = new();
			_appearedEvents         = new();
			_checkPoints            = new();
			_events                 = CreateTestEvents(10).ToArray();
		}

		public Position SecondCheckPoint { get; private set; }
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
					checkPointLowerBound: 1,
					checkPointUpperBound: 5,
					checkPointAfter: TimeSpan.FromSeconds(1),
					startFrom: Position.Start
				),
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

			await Task.WhenAll(_appeared.Task, Checkpointed()).WithTimeout();
			
			async Task Checkpointed() {
				bool firstCheckpointSet = false;
				await using var subscription = StreamsClient.SubscribeToStream(
					$"$persistentsubscription-$all::{Group}-checkpoint",
					FromStream.Start,
					userCredentials: TestCredentials.Root);
				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					if (!firstCheckpointSet) {
						FirstCheckPoint = resolvedEvent.Event.Data.ParsePosition();
						firstCheckpointSet = true;
					} else {
						SecondCheckPoint = resolvedEvent.Event.Data.ParsePosition();
						return;
					}
				}
			}
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
			return base.DisposeAsync();
		}
	}
}
