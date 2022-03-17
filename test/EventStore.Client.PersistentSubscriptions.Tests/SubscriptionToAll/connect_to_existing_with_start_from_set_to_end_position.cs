using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_start_from_set_to_end_position
		: IClassFixture<connect_to_existing_with_start_from_set_to_end_position.
			Fixture> {
		private readonly Fixture _fixture;
		private const string Group = "startfromend1";



		public connect_to_existing_with_start_from_set_to_end_position(
			Fixture fixture) {
			_fixture = fixture;
		}

		[SupportsPSToAll.Fact]
		public async Task the_subscription_gets_no_non_system_events() {
			await Assert.ThrowsAsync<TimeoutException>(() => _fixture.FirstNonSystemEvent.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			private readonly TaskCompletionSource<ResolvedEvent> _firstNonSystemEventSource;
			public Task<ResolvedEvent> FirstNonSystemEvent => _firstNonSystemEventSource.Task;
			private PersistentSubscription? _subscription;

			public Fixture() {
				_firstNonSystemEventSource = new TaskCompletionSource<ResolvedEvent>();
			}

			protected override async Task Given() {
				foreach (var @event in CreateTestEvents(10)) {
					await StreamsClient.AppendToStreamAsync("non-system-stream-" + Guid.NewGuid(),
						StreamState.Any, new[] {@event});
				}

				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: Position.End), userCredentials: TestCredentials.Root);
			}

			protected override async Task When() {
				_subscription = await Client.SubscribeToAllAsync(Group,
					async (subscription, e, r, ct) => {
						if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
							await subscription.Ack(e);
							return;
						}
						_firstNonSystemEventSource.TrySetResult(e);
						await subscription.Ack(e);
					}, (subscription, reason, ex) => {
						if (reason != SubscriptionDroppedReason.Disposed) {
							_firstNonSystemEventSource.TrySetException(ex!);
						}
					}, userCredentials: TestCredentials.Root);
			}

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
