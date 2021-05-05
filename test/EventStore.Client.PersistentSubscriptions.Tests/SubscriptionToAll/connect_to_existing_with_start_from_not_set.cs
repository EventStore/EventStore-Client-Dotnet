using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_start_from_not_set
		: IClassFixture<connect_to_existing_with_start_from_not_set.
			Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startfromend1";



		public connect_to_existing_with_start_from_not_set(
			Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_gets_no_non_system_events() {
			await Assert.ThrowsAsync<TimeoutException>(() => _fixture.FirstNonSystemEvent.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstNonSystemEventSource;
			public Task<ResolvedEvent> FirstNonSystemEvent => _firstNonSystemEventSource.Task;
			private PersistentSubscription _subscription;

			public Fixture() {
				_firstNonSystemEventSource = new TaskCompletionSource<ResolvedEvent>();
			}

			protected override async Task Given() {
				foreach (var @event in CreateTestEvents(10)) {
					await StreamsClient.AppendToStreamAsync("non-system-stream-" + Guid.NewGuid(),
						StreamState.Any, new[] {@event});
				}

				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(), TestCredentials.Root);
			}

			protected override async Task When() {
				_subscription = await Client.SubscribeToAllAsync(Group,
					(subscription, e, r, ct) => {
						if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
							return Task.CompletedTask;
						}
						_firstNonSystemEventSource.TrySetResult(e);
						return Task.CompletedTask;
					}, (subscription, reason, ex) => {
						if (reason != SubscriptionDroppedReason.Disposed) {
							_firstNonSystemEventSource.TrySetException(ex!);
						}
					}, TestCredentials.Root);
			}

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
