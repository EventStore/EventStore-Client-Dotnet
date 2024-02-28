namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_start_from_beginning_and_no_stream
	: IClassFixture<connect_to_existing_with_start_from_beginning_and_no_stream.Fixture> {
	private const string Group = "startinbeginning1";
	private const string Stream = nameof(connect_to_existing_with_start_from_beginning_and_no_stream);

	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_beginning_and_no_stream(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_event_zero_as_its_first_event() {
		var resolvedEvent = await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(StreamPosition.Start, resolvedEvent.Event.EventNumber);
		Assert.Equal(_fixture.EventId, resolvedEvent.Event.EventId);
	}

	public class Fixture : EventStoreClientFixture {
		public readonly EventData[] Events;
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			Events = CreateTestEvents().ToArray();
		}

		public Uuid EventId => Events.Single().EventId;

		protected override async Task Given() {
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.TestUser1);
		}

		protected override Task When() => StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}
			await base.DisposeAsync();
		}
	}
}
