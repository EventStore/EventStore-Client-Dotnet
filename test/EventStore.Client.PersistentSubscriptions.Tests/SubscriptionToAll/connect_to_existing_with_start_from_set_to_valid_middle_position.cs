namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_start_from_set_to_valid_middle_position
	: IClassFixture<connect_to_existing_with_start_from_set_to_valid_middle_position.Fixture> {
	private const string Group = "startfromvalid";

	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_set_to_valid_middle_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_the_event_at_the_specified_start_position_as_its_first_event() {
		var resolvedEvent = await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstAsync()
			.AsTask()
			.WithTimeout();
		Assert.Equal(_fixture.ExpectedEvent.OriginalPosition, resolvedEvent.Event.Position);
		Assert.Equal(_fixture.ExpectedEvent.Event.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(_fixture.ExpectedEvent.Event.EventStreamId, resolvedEvent.Event.EventStreamId);
	}

	public class Fixture : EventStoreClientFixture {
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }
		public ResolvedEvent ExpectedEvent { get; private set; }

		protected override async Task Given() {
			var events = await StreamsClient
				.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root)
				.ToArrayAsync();

			ExpectedEvent = events[events.Length / 2]; //just a random event in the middle of the results

			await Client.CreateToAllAsync(Group, new(startFrom: ExpectedEvent.OriginalPosition),
				userCredentials: TestCredentials.Root);
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
