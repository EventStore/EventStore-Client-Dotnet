namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class update_existing_with_check_point : IClassFixture<update_existing_with_check_point.Fixture> {
	private const string Group = "existing-with-check-point";

	private readonly Fixture _fixture;

	public update_existing_with_check_point(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public void resumes_from_check_point() {
		Assert.True(_fixture.Resumed.Event.Position > _fixture.CheckPoint);
	}

	public class Fixture : EventStoreClientFixture {
		private readonly List<ResolvedEvent> _appearedEvents;
		private readonly EventData[] _events;

		private EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? _subscription;
		private IAsyncEnumerator<PersistentSubscriptionMessage>? _enumerator;

		public ResolvedEvent Resumed { get; private set; }
		public Position CheckPoint { get; private set; }

		public Fixture() {
			_appearedEvents = new();
			_events = CreateTestEvents(5).ToArray();
		}

		protected override async Task Given() {
			foreach (var e in _events) {
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
			}

			await Client.CreateToAllAsync(Group,
				new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: Position.Start),
				userCredentials: TestCredentials.Root);

			_subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
			_enumerator = _subscription.Messages.GetAsyncEnumerator();
			await _enumerator.MoveNextAsync();

			await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoint().WithTimeout());

			return;

			async Task Subscribe() {
				while (await _enumerator.MoveNextAsync()) {
					if (_enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
						continue;
					}

					_appearedEvents.Add(resolvedEvent);
					await _subscription.Ack(resolvedEvent);
					if (_appearedEvents.Count == _events.Length) {
						break;
					}
				}
			}

			async Task WaitForCheckpoint() {
				await using var subscription = StreamsClient.SubscribeToStream(
					$"$persistentsubscription-$all::{Group}-checkpoint", FromStream.Start,
					userCredentials: TestCredentials.Root);
				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					CheckPoint = resolvedEvent.Event.Data.ParsePosition();
					return;
				}
			}		}

		protected override async Task When() {
			// Force restart of the subscription
			await Client.UpdateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);

			try {
				while (await _enumerator!.MoveNextAsync()) { }
			} catch (PersistentSubscriptionDroppedByServerException) { }

			foreach (var e in _events) {
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });
			}

			await using var subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
			Resumed = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
				.Select(e => e.ResolvedEvent)
				.Take(1)
				.FirstAsync()
				.AsTask()
				.WithTimeout();
		}

		public override async Task DisposeAsync() {
			if (_enumerator is not null) {
				await _enumerator.DisposeAsync();
			}

			if (_subscription is not null) {
				await _subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
