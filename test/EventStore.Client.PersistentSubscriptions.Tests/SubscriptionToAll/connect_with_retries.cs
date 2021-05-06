using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_with_retries
		: IClassFixture<connect_with_retries.Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "retries";


		public connect_with_retries(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task events_are_retried_until_success() {
			Assert.Equal(5, await _fixture.RetryCount.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<int> _retryCountSource;
			public Task<int> RetryCount => _retryCountSource.Task;
			private PersistentSubscription _subscription;

			public Fixture() {
				_retryCountSource = new TaskCompletionSource<int>();
			}

			protected override async Task Given() {
				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: Position.Start), TestCredentials.Root);
				_subscription = await Client.SubscribeToAllAsync(Group,
					async (subscription, e, r, ct) => {
						if (r > 4) {
							_retryCountSource.TrySetResult(r.Value);
							await subscription.Ack(e.Event.EventId);
						} else {
							await subscription.Nack(PersistentSubscriptionNakEventAction.Retry,
								"Not yet tried enough times", e);
						}
					}, autoAck: false, subscriptionDropped: (subscription, reason, ex) => {
						if (reason != SubscriptionDroppedReason.Disposed) {
							_retryCountSource.TrySetException(ex!);
						}
					}, userCredentials:TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
