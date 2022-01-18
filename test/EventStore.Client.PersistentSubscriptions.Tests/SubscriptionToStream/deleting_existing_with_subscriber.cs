using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class deleting_existing_with_subscriber
		: IClassFixture<deleting_existing_with_subscriber.Fixture> {
		private readonly Fixture _fixture;
		private const string Stream = nameof(deleting_existing_with_subscriber);

		public deleting_existing_with_subscriber(Fixture fixture) {
			_fixture = fixture;
		}

		public class Fixture : EventStoreClientFixture {
			public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _dropped.Task;
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _dropped;
			private PersistentSubscription? _subscription;

			public Fixture() {
				_dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
			}

			protected override async Task Given() {
				await Client.CreateToStreamAsync(Stream, "groupname123",
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);
				_subscription = await Client.SubscribeToStreamAsync(Stream, "groupname123",
					(_, _, _, _) => Task.CompletedTask,
					(_, r, e) => _dropped.TrySetResult((r, e)), TestCredentials.Root);
			}

			protected override Task When() =>
				Client.DeleteToStreamAsync(Stream, "groupname123",
					userCredentials: TestCredentials.Root);

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}

		[Fact]
		public async Task the_subscription_is_dropped() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
			Assert.Equal(Stream, ex.StreamName);
			Assert.Equal("groupname123", ex.GroupName);
		}

		[Fact (Skip = "Isn't this how it should work?")]
		public async Task the_subscription_is_dropped_with_not_found() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
			Assert.Equal(Stream, ex.StreamName);
			Assert.Equal("groupname123", ex.GroupName);
		}
	}
}
