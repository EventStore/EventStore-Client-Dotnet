namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_start_from_not_set_then_event_written
	: IClassFixture<connect_to_existing_with_start_from_not_set_then_event_written.Fixture> {
	const string Group = "startfromnotset2";

	readonly Fixture _fixture;

	public
		connect_to_existing_with_start_from_not_set_then_event_written(Fixture fixture) =>
		_fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_the_written_event_as_its_first_non_system_event() {
		var resolvedEvent = await _fixture.FirstNonSystemEvent.WithTimeout();
		Assert.Equal(_fixture.ExpectedEvent!.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(_fixture.ExpectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<ResolvedEvent> _firstNonSystemEventSource;

		public readonly EventData? ExpectedEvent;
		public readonly string     ExpectedStreamId;

		PersistentSubscription? _subscription;

		public Fixture() {
			_firstNonSystemEventSource = new();
			ExpectedEvent              = CreateTestEvents(1).First();
			ExpectedStreamId           = Guid.NewGuid().ToString();
		}

		public Task<ResolvedEvent> FirstNonSystemEvent => _firstNonSystemEventSource.Task;

		protected override async Task Given() {
			foreach (var @event in CreateTestEvents(10))
				await StreamsClient.AppendToStreamAsync(
					"non-system-stream-" + Guid.NewGuid(),
					StreamState.Any,
					new[] { @event }
				);

			await Client.CreateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);
			_subscription = await Client.SubscribeToAllAsync(
				Group,
				async (subscription, e, r, ct) => {
					if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
						await subscription.Ack(e);
						return;
					}

					_firstNonSystemEventSource.TrySetResult(e);
					await subscription.Ack(e);
				},
				(subscription, reason, ex) => {
					if (reason != SubscriptionDroppedReason.Disposed)
						_firstNonSystemEventSource.TrySetException(ex!);
				},
				TestCredentials.Root
			);
		}

		protected override async Task When() => await StreamsClient.AppendToStreamAsync(ExpectedStreamId, StreamState.NoStream, new[] { ExpectedEvent! });

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}