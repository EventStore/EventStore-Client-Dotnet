using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class a_nak_in_autoack_mode_drops_the_subscription
		: IClassFixture<a_nak_in_autoack_mode_drops_the_subscription.Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "naktest";
		private const string Stream = nameof(a_nak_in_autoack_mode_drops_the_subscription);

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
				await Client.CreateAsync(Stream, Group,
					new PersistentSubscriptionSettings(startFrom: StreamPosition.Start), TestCredentials.Root);
				_subscription = await Client.SubscribeAsync(Stream, Group,
					delegate {
						throw new Exception("test");
					}, (subscription, reason, ex) => _subscriptionDroppedSource.SetResult((reason, ex)), TestCredentials.Root);
			}

			protected override Task When() =>
				StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, Events);

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
