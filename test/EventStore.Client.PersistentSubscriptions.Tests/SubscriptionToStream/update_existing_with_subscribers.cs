namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class update_existing_with_subscribers
	: IClassFixture<update_existing_with_subscribers.Fixture> {
	const    string  Stream = nameof(update_existing_with_subscribers);
	const    string  Group  = "existing";
	readonly Fixture _fixture;

	public update_existing_with_subscribers(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task existing_subscriptions_are_dropped() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _droppedSource;
		PersistentSubscription?                                                _subscription;

		public Fixture() => _droppedSource = new();

		public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _droppedSource.Task;

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, CreateTestEvents());
			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

			_subscription = await Client.SubscribeToStreamAsync(
				Stream,
				Group,
				delegate { return Task.CompletedTask; },
				(_, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
				TestCredentials.Root
			);
		}

		protected override Task When() =>
			Client.UpdateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}