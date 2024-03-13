namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class update_existing_with_subscribers : IClassFixture<update_existing_with_subscribers.Fixture> {
	private const string Stream = nameof(update_existing_with_subscribers);
	private const string Group = "existing";
	private readonly Fixture _fixture;

	public update_existing_with_subscribers(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task existing_subscriptions_are_dropped() {
		var ex = await Assert.ThrowsAsync<PersistentSubscriptionDroppedByServerException>(async () => {
			while (await _fixture.Enumerator!.MoveNextAsync()) {
			}
		}).WithTimeout();

		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
	}

	public class Fixture : EventStoreClientFixture {
		private EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? _subscription;
		public IAsyncEnumerator<PersistentSubscriptionMessage>? Enumerator { get; private set; }

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, CreateTestEvents());
			await Client.CreateToStreamAsync(Stream, Group, new(), userCredentials: TestCredentials.Root);

			_subscription = Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.Root);
			Enumerator = _subscription.Messages.GetAsyncEnumerator();

			await Enumerator.MoveNextAsync();
		}

		protected override Task When() =>
			Client.UpdateToStreamAsync(Stream, Group, new(), userCredentials: TestCredentials.Root);

		public override async Task DisposeAsync() {
			if (Enumerator is not null) {
				await Enumerator.DisposeAsync();
			}

			if (_subscription is not null) {
				await _subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
