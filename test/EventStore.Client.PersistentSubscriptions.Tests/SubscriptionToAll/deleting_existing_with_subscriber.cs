using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class deleting_existing_with_subscriber
		: IClassFixture<deleting_existing_with_subscriber.Fixture> {
		private readonly Fixture _fixture;


		public deleting_existing_with_subscriber(Fixture fixture) {
			_fixture = fixture;
		}

		public class Fixture : EventStoreClientFixture {
			public Task<(SubscriptionDroppedReason reason, Exception exception)> Dropped => _dropped.Task;
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _dropped;
			private PersistentSubscription _subscription;

			public Fixture() {
				_dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			}

			protected override async Task Given() {
				await Client.CreateToAllAsync("groupname123",
					new PersistentSubscriptionSettings(),
					TestCredentials.Root);
				_subscription = await Client.SubscribeToAllAsync("groupname123",
					async (s, e, i, ct) => await s.Ack(e),
					(s, r, e) => _dropped.TrySetResult((r, e)), TestCredentials.Root);
				// todo: investigate why this test is flaky without this delay
				await Task.Delay(500);
			}

			protected override Task When() =>
				Client.DeleteToAllAsync("groupname123",
					TestCredentials.Root);

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
			Assert.Equal(SystemStreams.AllStream, ex.StreamName);
			Assert.Equal("groupname123", ex.GroupName);
		}

		[Fact (Skip = "Isn't this how it should work?")]
		public async Task the_subscription_is_dropped_with_not_found() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
			Assert.Equal(SystemStreams.AllStream, ex.StreamName);
			Assert.Equal("groupname123", ex.GroupName);
		}
	}
}
