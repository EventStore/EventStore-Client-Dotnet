namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_with_retries : IClassFixture<connect_with_retries.Fixture> {
	private const string Group = "retries";
	private readonly Fixture _fixture;

	public connect_with_retries(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
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
		public EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? Subscription { get; private set; }

		protected override async Task Given() {
			await Client.CreateToAllAsync(Group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

			Subscription = Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);
		}

		protected override Task When() => Task.CompletedTask;

		public override async Task DisposeAsync() {
			if (Subscription is not null) {
				await Subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
