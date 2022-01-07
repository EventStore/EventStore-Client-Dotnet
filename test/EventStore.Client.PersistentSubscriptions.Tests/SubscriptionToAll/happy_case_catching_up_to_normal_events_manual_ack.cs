using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class happy_case_catching_up_to_normal_events_manual_ack :
		IClassFixture<happy_case_catching_up_to_normal_events_manual_ack.Fixture> {

		private const string Group = nameof(Group);
		private const int BufferCount = 10;
		private const int EventWriteCount = BufferCount * 2;

		private readonly Fixture _fixture;

		public happy_case_catching_up_to_normal_events_manual_ack(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task Test() {
			await _fixture.EventsReceived.WithTimeout();
		}

		public class Fixture : EventStoreClientFixture {
			private readonly EventData[] _events;
			private readonly TaskCompletionSource<bool> _eventsReceived;
			public Task EventsReceived => _eventsReceived.Task;

			private PersistentSubscription _subscription;
			private int _eventReceivedCount;

			public Fixture() {
				_events = CreateTestEvents(EventWriteCount).ToArray();
				_eventsReceived = new TaskCompletionSource<bool>();
			}

			protected override async Task Given() {
				foreach (var e in _events) {
					await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] {e});
				}

				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(startFrom: Position.Start, resolveLinkTos: true),
					TestCredentials.Root);
				_subscription = await Client.SubscribeToAllAsync(Group,
					async(subscription, e, retryCount, ct) => {
						await subscription.Ack(e);

						if (e.OriginalStreamId.StartsWith("test-")
						    && Interlocked.Increment(ref _eventReceivedCount) == _events.Length) {
							_eventsReceived.TrySetResult(true);
						}
					}, (s, r, e) => {
						if (e != null) {
							_eventsReceived.TrySetException(e);
						}
					},
					bufferSize: BufferCount,
					userCredentials: TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
