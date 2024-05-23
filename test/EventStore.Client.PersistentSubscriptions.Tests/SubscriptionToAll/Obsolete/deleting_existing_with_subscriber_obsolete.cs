namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class deleting_existing_with_subscriber_obsolete
	: IClassFixture<deleting_existing_with_subscriber_obsolete.Fixture> {
	readonly Fixture _fixture;

	public deleting_existing_with_subscriber_obsolete(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_is_dropped() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal("groupname123", ex.GroupName);
	}

	[Fact(Skip = "Isn't this how it should work?")]
	public async Task the_subscription_is_dropped_with_not_found() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal("groupname123", ex.GroupName);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _dropped;
		PersistentSubscription?                                                _subscription;

		public Fixture() => _dropped = new();

		public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _dropped.Task;

		protected override async Task Given() {
			await Client.CreateToAllAsync(
				"groupname123",
				new(),
				userCredentials: TestCredentials.Root
			);

			_subscription = await Client.SubscribeToAllAsync(
				"groupname123",
				async (s, e, i, ct) => await s.Ack(e),
				(s, r, e) => _dropped.TrySetResult((r, e)),
				TestCredentials.Root
			);

			// todo: investigate why this test is flaky without this delay
			await Task.Delay(500);
		}

		protected override Task When() =>
			Client.DeleteToAllAsync(
				"groupname123",
				userCredentials: TestCredentials.Root
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
