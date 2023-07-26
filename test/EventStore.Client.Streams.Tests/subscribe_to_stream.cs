using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class subscribe_to_stream : IClassFixture<subscribe_to_stream.Fixture> {
		private readonly Fixture _fixture;

		public subscribe_to_stream(Fixture fixture, ITestOutputHelper outputHelper) {
			_fixture = fixture;
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task Callback_subscribe_to_non_existing_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			Assert.False(appeared.Task.IsCompleted);

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				appeared.TrySetResult(true);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex)
				=> dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_subscribe_to_non_existing_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

			Assert.False(appeared.Task.IsCompleted);

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				appeared.TrySetResult(true);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_subscribe_to_non_existing_stream_then_get_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				appeared.TrySetResult(true);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex)
				=> dropped.SetResult((reason, ex));
		}

		[Fact]
		public async Task Iterator_subscribe_to_non_existing_stream_then_get_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			Assert.True(await appeared.Task.WithTimeout());

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				appeared.TrySetResult(true);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_allow_multiple_subscriptions_to_same_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var appeared = new TaskCompletionSource<bool>();

			int appearedCount = 0;

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());
			using var s1 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared).WithTimeout();
			using var s2 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared).WithTimeout();

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

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());
			
			var s1 = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(s1, EventAppeared, null);
			
			var s2 = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(s2, EventAppeared, null);

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

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_client_stops_reading_messages_when_subscription_disposed() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var dropped = new TaskCompletionSource<Exception?>();
			
			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
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
		public async Task Callback_calls_subscription_dropped_when_error_processing_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
			var expectedException = new Exception("Error");

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.SubscriberError, reason);
			Assert.Same(expectedException, ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) =>
				Task.FromException(expectedException);

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_client_stops_reading_messages_when_error_processing_event() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var dropped = new TaskCompletionSource<Exception?>();
			var expectedException = new Exception("Error");
			int numTimesCalled = 0;
			
			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));

			var ex = await dropped.Task.WithTimeout();
			Assert.Same(expectedException, ex);
			
			Assert.Equal(1, numTimesCalled);

			Task EventAppeared(ResolvedEvent e) {
				numTimesCalled++;
				return Task.FromException(expectedException);
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_reads_all_existing_events_and_keep_listening_to_new_ones() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
					SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
				.WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				try {
					Assert.Equal(enumerator.Current.EventId, e.OriginalEvent.EventId);
					if (!enumerator.MoveNext()) {
						appeared.TrySetResult(true);
					}
				} catch (Exception ex) {
					appeared.TrySetException(ex);
					throw;
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_reads_all_existing_events_and_keep_listening_to_new_ones() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";
			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, beforeEvents);

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
				
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents);

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				try {
					Assert.Equal(enumerator.Current.EventId, e.OriginalEvent.EventId);
					if (!enumerator.MoveNext()) {
						appeared.TrySetResult(true);
					}
				} catch (Exception ex) {
					appeared.TrySetException(ex);
					throw;
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_catches_deletions() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var _ = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared, false,
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

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);
			var ex = await dropped.Task.WithTimeout();

			var sdex = Assert.IsType<StreamDeletedException>(ex);
			Assert.Equal(stream, sdex.Stream);

			Task EventAppeared(ResolvedEvent e) => Task.CompletedTask;

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}


		public class Fixture : EventStoreClientFixture {
			public EventData[] Events { get; }

			public Fixture() {
				Events = CreateTestEvents(10).ToArray();
			}

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
