namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class update_existing_with_check_point_filtered_obsolete
	: IClassFixture<update_existing_with_check_point_filtered_obsolete.Fixture> {
	const string Group = "existing-with-check-point-filtered";

	readonly Fixture _fixture;

	public update_existing_with_check_point_filtered_obsolete(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task resumes_from_check_point() {
		var resumedEvent = await _fixture.Resumed.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.True(resumedEvent.Event.Position > _fixture.CheckPoint);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<bool>                                    _appeared;
		readonly List<ResolvedEvent>                                           _appearedEvents;
		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _droppedSource;
		readonly EventData[]                                                   _events;
		readonly TaskCompletionSource<ResolvedEvent>                           _resumedSource;

		PersistentSubscription? _firstSubscription;
		PersistentSubscription? _secondSubscription;

		public Fixture() {
			_droppedSource    = new();
			_resumedSource    = new();
			_appeared         = new();
			_appearedEvents   = new();
			_events           = CreateTestEvents(5).ToArray();
		}

		public Task<ResolvedEvent> Resumed => _resumedSource.Task;

		public Position CheckPoint { get; private set; }

		protected override async Task Given() {
			foreach (var e in _events)
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });

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

			_firstSubscription = await Client.SubscribeToAllAsync(
				Group,
				async (s, e, r, ct) => {
					_appearedEvents.Add(e);

					if (_appearedEvents.Count == _events.Length)
						_appeared.TrySetResult(true);

					await s.Ack(e);
				},
				(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
				TestCredentials.Root
			);

			await Task.WhenAll(_appeared.Task, Checkpointed()).WithTimeout();

			return;

			async Task Checkpointed() {
				await using var subscription = StreamsClient.SubscribeToStream(
					$"$persistentsubscription-$all::{Group}-checkpoint",
					FromStream.Start,
					userCredentials: TestCredentials.Root);
				await foreach (var message in subscription.Messages) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}
					CheckPoint = resolvedEvent.Event.Data.ParsePosition();
					return;
				}
			}
		}

		protected override async Task When() {
			// Force restart of the subscription
			await Client.UpdateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);

			await _droppedSource.Task.WithTimeout();

			_secondSubscription = await Client.SubscribeToAllAsync(
				Group,
				async (s, e, r, ct) => {
					_resumedSource.TrySetResult(e);
					await s.Ack(e);
					s.Dispose();
				},
				(_, reason, ex) => {
					if (ex is not null)
						_resumedSource.TrySetException(ex);
					else
						_resumedSource.TrySetResult(default);
				},
				TestCredentials.Root
			);

			foreach (var e in _events)
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });
		}

		public override Task DisposeAsync() {
			_firstSubscription?.Dispose();
			_secondSubscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
