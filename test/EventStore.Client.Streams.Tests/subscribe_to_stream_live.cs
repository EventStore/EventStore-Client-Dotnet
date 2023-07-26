using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class subscribe_to_stream_live : IClassFixture<subscribe_to_stream_live.Fixture> {
		private readonly Fixture _fixture;

		public subscribe_to_stream_live(Fixture fixture, ITestOutputHelper outputHelper) {
			_fixture = fixture;
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task Callback_does_not_read_existing_events_but_keep_listening_to_new_ones() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<StreamPosition>();
			var dropped = new TaskCompletionSource<bool>();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents());

			using var _ = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, (_, e, _) => {
					appeared.TrySetResult(e.OriginalEventNumber);
					return Task.CompletedTask;
				}, false, (s, reason, ex) => dropped.TrySetResult(true))
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(0),
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamPosition(1), await appeared.Task.WithTimeout());
		}
		
		[Fact]
		public async Task Iterator_does_not_read_existing_events_but_keep_listening_to_new_ones() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<StreamPosition>();
			var dropped = new TaskCompletionSource<bool>();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents());

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			ReadMessages(subscription, e => {
					appeared.TrySetResult(e.OriginalEventNumber);
					return Task.CompletedTask;
				}, _ => dropped.TrySetResult(true));

			await _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(0),
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamPosition(1), await appeared.Task.WithTimeout());
		}

		[Fact]
		public async Task Callback_subscribe_to_non_existing_stream_and_then_catch_new_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<bool>();

			using var _ = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, (_, _, _) => {
					appeared.TrySetResult(true);
					return Task.CompletedTask;
				}, false, (s, reason, ex) => dropped.TrySetResult(true))
				.WithTimeout();
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());
		}
		
		[Fact]
		public async Task Iterator_subscribe_to_non_existing_stream_and_then_catch_new_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<bool>();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			ReadMessages(subscription, _ => {
				appeared.TrySetResult(true);
				return Task.CompletedTask;
			}, _ => dropped.TrySetResult(true));

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());
		}

		[Fact]
		public async Task Callback_allow_multiple_subscriptions_to_same_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var appeared = new TaskCompletionSource<bool>();

			int appearedCount = 0;

			using var s1 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
				.WithTimeout();
			using var s2 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
				.WithTimeout();
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (++appearedCount == 2) {
					appeared.TrySetResult(true);
				}

				return Task.CompletedTask;
			}
		}
		
		[Fact]
		public async Task Iterator_allow_multiple_subscriptions_to_same_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var appeared = new TaskCompletionSource<bool>();

			int appearedCount = 0;

			var s1 = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			ReadMessages(s1, EventAppeared, null);
			
			var s2 = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			ReadMessages(s2, EventAppeared, null);
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());

			Task EventAppeared(ResolvedEvent e) {
				if (++appearedCount == 2) {
					appeared.TrySetResult(true);
				}

				return Task.CompletedTask;
			}
		}

		[Fact]
		public async Task Callback_calls_subscription_dropped_when_disposed() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var _ = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				s.Dispose();
				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex)
				=> dropped.SetResult((reason, ex));

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Null(ex);
			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		}

		[Fact]
		public async Task Iterator_client_stops_reading_messages_when_subscription_disposed() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var dropped = new TaskCompletionSource<Exception?>();
			
			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			var testEvent = _fixture.CreateTestEvents(1).First();
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();
			
			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);
			
			// new event after subscription is disposed
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, new[]{testEvent});

			Task EventAppeared(ResolvedEvent e) {
				return testEvent.EventId.Equals(e.OriginalEvent.EventId) ? Task.FromException(new Exception("Subscription not dropped")) : Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_catches_deletions() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var _ = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);
			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
			var sdex = Assert.IsType<StreamDeletedException>(ex);
			Assert.Equal(stream, sdex.Stream);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_catches_deletions() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var dropped = new TaskCompletionSource<Exception?>();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.End);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);
			var ex = await dropped.Task.WithTimeout();

			var sdex = Assert.IsType<StreamDeletedException>(ex);
			Assert.Equal(stream, sdex.Stream);

			Task EventAppeared(ResolvedEvent e) => Task.CompletedTask;

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
		
		async void ReadMessages(EventStoreClient.SubscriptionResult subscription, Func<ResolvedEvent, Task> eventAppeared, Action<Exception?>? subscriptionDropped) {
			Exception? exception = null;
			try {
				await foreach (var message in subscription.Messages) {
					if (message is StreamMessage.Event eventMessage) {
						await eventAppeared(eventMessage.ResolvedEvent);
					}
				}
			} catch (Exception ex) {
				exception = ex;
			}

			//allow some time for subscription cleanup and chance for exception to be raised
			await Task.Delay(100);

			try {
				//subscription.SubscriptionState will throw exception if some problem occurred for the subscription
				Assert.Equal(SubscriptionState.Disposed, subscription.SubscriptionState);
				subscriptionDropped?.Invoke(exception);
			} catch (Exception ex) {
				subscriptionDropped?.Invoke(ex);
			}
		}
	}
}
