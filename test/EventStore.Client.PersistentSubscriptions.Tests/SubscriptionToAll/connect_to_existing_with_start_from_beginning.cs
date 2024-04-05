namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_start_from_beginning
	: IClassFixture<connect_to_existing_with_start_from_beginning.Fixture> {
	private const string Group = "startfrombeginning";
	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_beginning(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_event_zero_as_its_first_event() {
		var resolvedEvent = await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(_fixture.Events[0].Event.EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public ResolvedEvent[] Events { get; private set; } = [];

		protected override async Task Given() {
			//append 10 events to random streams to make sure we have at least 10 events in the transaction file
			foreach (var @event in CreateTestEvents(10)) {
				await StreamsClient.AppendToStreamAsync(Guid.NewGuid().ToString(), StreamState.NoStream,
					new[] { @event });
			}

			Events = await StreamsClient
				.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root)
				.ToArrayAsync();

			await Client.CreateToAllAsync(Group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);
		}

		protected override Task When() {
			Subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);

			return Task.CompletedTask;
		}

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
