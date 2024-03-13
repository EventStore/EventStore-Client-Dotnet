namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

[Obsolete]
public class connect_to_existing_with_start_from_beginning_obsolete : IClassFixture<connect_to_existing_with_start_from_beginning_obsolete.Fixture> {
	const string Group = "startfrombeginning";

	readonly Fixture _fixture;

	public connect_to_existing_with_start_from_beginning_obsolete(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_event_zero_as_its_first_event() {
		var resolvedEvent = await _fixture.FirstEvent.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(_fixture.Events![0].Event.EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;

		PersistentSubscription? _subscription;

		public Fixture() => _firstEventSource = new();

		public ResolvedEvent[]? Events { get; set; }

		public Task<ResolvedEvent> FirstEvent => _firstEventSource.Task;

		protected override async Task Given() {
			//append 10 events to random streams to make sure we have at least 10 events in the transaction file
			foreach (var @event in CreateTestEvents(10))
				await StreamsClient.AppendToStreamAsync(Guid.NewGuid().ToString(), StreamState.NoStream, new[] { @event });

			Events = await StreamsClient.ReadAllAsync(
				Direction.Forwards,
				Position.Start,
				10,
				userCredentials: TestCredentials.Root
			).ToArrayAsync();

			await Client.CreateToAllAsync(
				Group,
				new(startFrom: Position.Start),
				userCredentials: TestCredentials.Root
			);
		}

		protected override async Task When() =>
			_subscription = await Client.SubscribeToAllAsync(
				Group,
				async (subscription, e, r, ct) => {
					_firstEventSource.TrySetResult(e);
					await subscription.Ack(e);
				},
				(subscription, reason, ex) => {
					if (reason != SubscriptionDroppedReason.Disposed)
						_firstEventSource.TrySetException(ex!);
				},
				TestCredentials.Root
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
