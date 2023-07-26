using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class subscribe_to_stream_with_revision : IAsyncLifetime {
		private readonly Fixture _fixture;

		public subscribe_to_stream_with_revision(ITestOutputHelper outputHelper) {
			_fixture = new Fixture();
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
				.SubscribeToStreamAsync(stream, FromStream.After(StreamPosition.Start), EventAppeared,
					false, SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents(2));

			Assert.True(await appeared.Task.WithTimeout());

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();
			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (e.OriginalEvent.EventNumber == StreamPosition.Start) {
					appeared.TrySetException(new Exception());
				} else {
					appeared.TrySetResult(true);
				}

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

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.After(StreamPosition.Start));
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));

			Assert.True(await appeared.Task.WithTimeout());

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				if (e.OriginalEvent.EventNumber == StreamPosition.Start) {
					appeared.TrySetException(new Exception());
				} else {
					appeared.TrySetResult(true);
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_allow_multiple_subscriptions_to_same_stream() {
			var stream = $"{_fixture.GetStreamName()}_{Guid.NewGuid()}";

			var appeared = new TaskCompletionSource<bool>();
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));
			
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1).FirstOrDefaultAsync();
			int appearedCount = 0;

			using var s1 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.After(firstEvent.OriginalEventNumber), EventAppeared)
				.WithTimeout();
			using var s2 = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.After(firstEvent.OriginalEventNumber), EventAppeared)
				.WithTimeout();

			Assert.True(await appeared.Task.WithTimeout());

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (e.OriginalEvent.EventNumber == firstEvent.OriginalEventNumber) {
					appeared.TrySetException(new Exception());
					return Task.CompletedTask;
				}

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
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1).FirstOrDefaultAsync();
			
			int appearedCount = 0;

			var s1 = _fixture.Client.SubscribeToStream(stream, FromStream.After(firstEvent.OriginalEventNumber));
			ReadMessages(s1, EventAppeared, null);
			
			var s2 = _fixture.Client.SubscribeToStream(stream, FromStream.After(firstEvent.OriginalEventNumber));
			ReadMessages(s2, EventAppeared, null);

			Assert.True(await appeared.Task.WithTimeout());

			Task EventAppeared(ResolvedEvent e) {
				if (e.OriginalEvent.EventNumber == firstEvent.OriginalEventNumber) {
					appeared.TrySetException(new Exception());
					return Task.CompletedTask;
				}

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
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.After(firstEvent.OriginalEventNumber), EventAppeared, false,
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
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.After(firstEvent.OriginalEventNumber));
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

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(3));
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();

			using var subscription = await _fixture.Client.SubscribeToStreamAsync(stream,
					FromStream.After(firstEvent.OriginalEventNumber),
					EventAppeared, false, SubscriptionDropped)
				.WithTimeout();

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
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(3));
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();
			
			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.After(firstEvent.OriginalEventNumber));
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

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

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, beforeEvents);
			
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync(stream, FromStream.After(firstEvent.OriginalEventNumber), EventAppeared,
					false, SubscriptionDropped)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents);

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

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
				dropped.SetResult((reason, ex));
			}
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

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, beforeEvents);
			
			var firstEvent = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.FirstOrDefaultAsync();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.After(firstEvent.OriginalEventNumber));
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

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}

		public Task InitializeAsync() => _fixture.InitializeAsync();

		public Task DisposeAsync() => _fixture.DisposeAsync();
		
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
