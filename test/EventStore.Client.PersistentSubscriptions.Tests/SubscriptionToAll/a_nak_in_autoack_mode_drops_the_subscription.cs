using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class a_nak_in_autoack_mode_drops_the_subscription
		: IClassFixture<a_nak_in_autoack_mode_drops_the_subscription.Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "naktest";


		public a_nak_in_autoack_mode_drops_the_subscription(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_dropped() {
			var (reason, exception) = await _fixture.SubscriptionDropped.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.Equal(SubscriptionDroppedReason.SubscriberError, reason);
			var ex = Assert.IsType<Exception>(exception);
			Assert.Equal("test", ex.Message);
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _subscriptionDroppedSource;

			public Task<(SubscriptionDroppedReason reason, Exception exception)> SubscriptionDropped =>
				_subscriptionDroppedSource.Task;

			public readonly EventData[] Events;
			private PersistentSubscription _subscription;

			public Fixture() {
				_subscriptionDroppedSource = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
				Events = CreateTestEvents().ToArray();
			}

			protected override async Task Given() {
				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: Position.Start), TestCredentials.Root);
				_subscription = await Client.SubscribeToAllAsync(Group,
					delegate {
						throw new Exception("test");
					}, (subscription, reason, ex) => _subscriptionDroppedSource.SetResult((reason, ex)), TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
