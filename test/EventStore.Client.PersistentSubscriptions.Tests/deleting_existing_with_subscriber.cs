using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class deleting_existing_with_subscriber
		: IClassFixture<deleting_existing_with_subscriber.Fixture> {
		private const string Stream = nameof(deleting_existing_with_subscriber);
		private const string Group = "existing";
		private readonly Fixture _fixture;

		public deleting_existing_with_subscriber(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_is_dropped() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
			Assert.Equal(Stream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		[Fact (Skip = "Isn't this how it should work?")]
		public async Task the_subscription_is_dropped_with_not_found() {
			var (reason, exception) = await _fixture.Dropped.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
			Assert.Equal(Stream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			public Task<(SubscriptionDroppedReason reason, Exception exception)> Dropped => _droppedSource.Task;
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _droppedSource;
			private PersistentSubscription _subscription;

			public Fixture() {
				_droppedSource = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			}

			protected override async Task Given() {
				await Client.CreateAsync(Stream, Group,
					new PersistentSubscriptionSettings(),
					TestCredentials.Root);
				_subscription = await Client.SubscribeAsync(Stream, Group,
					delegate { return Task.CompletedTask; },
					(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)), TestCredentials.Root);
			}

			protected override Task When() =>
				Client.DeleteAsync(Stream, Group,
					TestCredentials.Root);

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
