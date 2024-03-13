namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_with_retries : IClassFixture<connect_with_retries.Fixture> {
	private const string Group  = "retries";
	private const string Stream = nameof(connect_with_retries);

	private readonly Fixture _fixture;

	public connect_with_retries(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task events_are_retried_until_success() {
		var retryCount = await _fixture.Subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.SelectAwait(async e => {
				if (e.RetryCount > 4) {
					await _fixture.Subscription.Ack(e.ResolvedEvent);
				} else {
					await _fixture.Subscription.Nack(PersistentSubscriptionNakEventAction.Retry,
						"Not yet tried enough times", e.ResolvedEvent);
				}

				return e.RetryCount;
			})
			.Where(retryCount => retryCount > 4)
			.FirstOrDefaultAsync()
			.AsTask()
			.WithTimeout();
		
		Assert.Equal(5, retryCount);
	}

	public class Fixture : EventStoreClientFixture {

		public readonly EventData[] Events;

		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		public Fixture() {
			Events = CreateTestEvents().ToArray();
		}

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
			await Client.CreateToStreamAsync(Stream, Group, new(startFrom: StreamPosition.Start),
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
