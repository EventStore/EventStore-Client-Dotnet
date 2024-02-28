namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written
	: IClassFixture<connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written.Fixture> {
	private const string Group = "startinbeginning1";
	private const string Stream = nameof(connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written);
	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written(Fixture fixture) =>
		_fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_the_written_event_as_its_first_event() {
		var resolvedEvent = await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();
		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(_fixture.Events.Last().EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		public readonly EventData[] Events;
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			Events = CreateTestEvents(11).ToArray();
		}

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events.Take(10));
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(startFrom: new StreamPosition(10)),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToStream(
				Stream,
				Group,
				userCredentials: TestCredentials.TestUser1);
		}

		protected override Task When() =>
			StreamsClient.AppendToStreamAsync(Stream, new StreamRevision(9), Events.Skip(10));

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
