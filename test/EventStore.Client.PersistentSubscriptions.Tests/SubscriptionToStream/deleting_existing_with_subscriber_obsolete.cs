namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

[Obsolete]
public class deleting_existing_with_subscriber_obsolete
	: IClassFixture<deleting_existing_with_subscriber_obsolete.Fixture> {
	const    string  Stream = nameof(deleting_existing_with_subscriber_obsolete);
	readonly Fixture _fixture;

	public deleting_existing_with_subscriber_obsolete(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_is_dropped() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

#if NET
		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal("groupname123", ex.GroupName);
#endif
	}

	[Fact(Skip = "Isn't this how it should work?")]
	public async Task the_subscription_is_dropped_with_not_found() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal("groupname123", ex.GroupName);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _dropped;
		PersistentSubscription?                                                _subscription;

		public Fixture() => _dropped = new();

		public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _dropped.Task;

		protected override async Task Given() {
			await Client.CreateToStreamAsync(
				Stream,
				"groupname123",
				new(),
				userCredentials: TestCredentials.Root
			);

			_subscription = await Client.SubscribeToStreamAsync(
				Stream,
				"groupname123",
				(_, _, _, _) => Task.CompletedTask,
				(_, r, e) => _dropped.TrySetResult((r, e)),
				TestCredentials.Root
			);
		}

		protected override Task When() =>
			Client.DeleteToStreamAsync(
				Stream,
				"groupname123",
				userCredentials: TestCredentials.Root
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
