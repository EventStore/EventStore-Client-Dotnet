namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class connect_to_existing_with_start_from_set_to_valid_middle_position_obsolete
	: IClassFixture<connect_to_existing_with_start_from_set_to_valid_middle_position_obsolete.Fixture> {
	const string Group = "startfromvalid";

	readonly Fixture _fixture;

	public connect_to_existing_with_start_from_set_to_valid_middle_position_obsolete(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_the_event_at_the_specified_start_position_as_its_first_event() {
		var resolvedEvent = await _fixture.FirstEvent.WithTimeout();
		Assert.Equal(_fixture.ExpectedEvent.OriginalPosition, resolvedEvent.Event.Position);
		Assert.Equal(_fixture.ExpectedEvent.Event.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(_fixture.ExpectedEvent.Event.EventStreamId, resolvedEvent.Event.EventStreamId);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<ResolvedEvent> _firstEventSource;
		PersistentSubscription?                      _subscription;

		public Fixture() => _firstEventSource = new();

		public Task<ResolvedEvent> FirstEvent    => _firstEventSource.Task;
		public ResolvedEvent       ExpectedEvent { get; private set; }

		protected override async Task Given() {
			var events = await StreamsClient.ReadAllAsync(
				Direction.Forwards,
				Position.Start,
				10,
				userCredentials: TestCredentials.Root
			).ToArrayAsync();

			ExpectedEvent = events[events.Length / 2]; //just a random event in the middle of the results

			await Client.CreateToAllAsync(
				Group,
				new(startFrom: ExpectedEvent.OriginalPosition),
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
