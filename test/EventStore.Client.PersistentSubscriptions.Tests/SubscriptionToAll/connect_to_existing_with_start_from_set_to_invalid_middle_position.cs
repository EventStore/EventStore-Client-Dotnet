using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_start_from_set_to_invalid_middle_position
		: IClassFixture<connect_to_existing_with_start_from_set_to_invalid_middle_position.
			Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startfrominvalid1";



		public connect_to_existing_with_start_from_set_to_invalid_middle_position(
			Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_is_dropped() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _dropped;
			public Task<(SubscriptionDroppedReason reason, Exception exception)> Dropped => _dropped.Task;

			private PersistentSubscription _subscription;

			public Fixture() {
				_dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			}

			protected override async Task Given() {
				var invalidPosition = new Position(1L, 1L);
				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: invalidPosition), TestCredentials.Root);
			}

			protected override async Task When() {
				_subscription = await Client.SubscribeToAllAsync(Group,
					(subscription, e, r, ct) => Task.CompletedTask,
					(subscription, reason, ex) => {
						_dropped.TrySetResult((reason, ex));
					}, TestCredentials.Root);
			}

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
