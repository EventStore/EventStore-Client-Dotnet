namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_start_from_not_set_and_events_in_it
	: IClassFixture<connect_to_existing_with_start_from_not_set_and_events_in_it.Fixture> {
	private const string Group = "startinbeginning1";

	private const string Stream = nameof(connect_to_existing_with_start_from_not_set_and_events_in_it);
	private readonly Fixture _fixture;

	public connect_to_existing_with_start_from_not_set_and_events_in_it(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_gets_no_events() =>
		await Assert.ThrowsAsync<TimeoutException>(
			() => _fixture.Subscription!.Messages.AnyAsync(message => message is PersistentSubscriptionMessage.Event)
				.AsTask().WithTimeout(TimeSpan.FromMilliseconds(250)));

	public class Fixture : EventStoreClientFixture {
		public readonly EventData[] Events;
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() { 
			Events = CreateTestEvents(10).ToArray();
		}

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);
		}

		protected override Task When() {
			Subscription = Client.SubscribeToStream(
				Stream,
				Group,
				userCredentials: TestCredentials.TestUser1);
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
