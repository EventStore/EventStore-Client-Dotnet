namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class
	connect_to_existing_with_start_from_set_to_end_position_then_event_written
	: IClassFixture<connect_to_existing_with_start_from_set_to_end_position_then_event_written.Fixture> {
	private const string Group = "startfromnotset2";
	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_set_to_end_position_then_event_written(Fixture fixture) =>
		_fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_gets_the_written_event_as_its_first_non_system_event() {
		var resolvedEvent = await  _fixture.Subscription!.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.Where(resolvedEvent => !SystemStreams.IsSystemStream(resolvedEvent.OriginalStreamId))
			.FirstAsync()
			.AsTask()
			.WithTimeout();
		Assert.Equal(_fixture.ExpectedEvent.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(_fixture.ExpectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	public class Fixture : EventStoreClientFixture {
		public readonly EventData ExpectedEvent;
		public readonly string ExpectedStreamId;
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			ExpectedEvent = CreateTestEvents(1).First();
			ExpectedStreamId = Guid.NewGuid().ToString();
		}

		protected override async Task Given() {
			foreach (var @event in CreateTestEvents(10)) {
				await StreamsClient.AppendToStreamAsync("non-system-stream-" + Guid.NewGuid(), StreamState.Any,
					new[] { @event });
			}

			await Client.CreateToAllAsync(Group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);
			Subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
		}

		protected override async Task When() =>
			await StreamsClient.AppendToStreamAsync(ExpectedStreamId, StreamState.NoStream, new[] { ExpectedEvent });

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
