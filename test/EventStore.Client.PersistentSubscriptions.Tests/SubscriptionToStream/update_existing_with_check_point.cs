namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class update_existing_with_check_point
	: IClassFixture<update_existing_with_check_point.Fixture> {
	const    string  Stream = nameof(update_existing_with_check_point);
	const    string  Group  = "existing-with-check-point";
	readonly Fixture _fixture;

	public update_existing_with_check_point(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task resumes_from_check_point() {
		var resumedEvent = await _fixture.Resumed.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(_fixture.CheckPoint.Next(), resumedEvent.Event.EventNumber);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<bool>          _appeared;
		readonly List<ResolvedEvent>                 _appearedEvents;

		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _droppedSource;
		readonly EventData[]                                                   _events;
		readonly TaskCompletionSource<ResolvedEvent>                           _resumedSource;
		PersistentSubscription?                                                _firstSubscription;
		PersistentSubscription?                                                _secondSubscription;

		public Fixture() {
			_droppedSource    = new();
			_resumedSource    = new();
			_appeared         = new();
			_appearedEvents   = new();
			_events           = CreateTestEvents(5).ToArray();
		}

		public Task<ResolvedEvent> Resumed    => _resumedSource.Task;
		public StreamPosition      CheckPoint { get; private set; }

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, _events);

			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(
					checkPointLowerBound: 5,
					checkPointAfter: TimeSpan.FromSeconds(1),
					startFrom: StreamPosition.Start
				),
				userCredentials: TestCredentials.Root
			);

			var checkPointStream = $"$persistentsubscription-{Stream}::{Group}-checkpoint";

			_firstSubscription = await Client.SubscribeToStreamAsync(
				Stream,
				Group,
				async (s, e, _, _) => {
					_appearedEvents.Add(e);
					await s.Ack(e);

					if (_appearedEvents.Count == _events.Length)
						_appeared.TrySetResult(true);
				},
				(_, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
				TestCredentials.Root
			);

			await Task.WhenAll(_appeared.Task.WithTimeout(), Checkpointed());

			return;

			async Task Checkpointed() {
				await using var subscription = StreamsClient.SubscribeToStream(checkPointStream, FromStream.Start,
					userCredentials: TestCredentials.Root);
				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}
					CheckPoint = resolvedEvent.Event.Data.ParseStreamPosition();
					return;
				}

				throw new InvalidOperationException();
			}
		}

		protected override async Task When() {
			// Force restart of the subscription
			await Client.UpdateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

			await _droppedSource.Task.WithTimeout();

			_secondSubscription = await Client.SubscribeToStreamAsync(
				Stream,
				Group,
				async (s, e, _, _) => {
					_resumedSource.TrySetResult(e);
					await s.Ack(e);
				},
				(_, reason, ex) => {
					if (ex is not null)
						_resumedSource.TrySetException(ex);
					else
						_resumedSource.TrySetResult(default);
				},
				TestCredentials.Root
			);

			await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents(1));
		}

		public override Task DisposeAsync() {
			_firstSubscription?.Dispose();
			_secondSubscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
