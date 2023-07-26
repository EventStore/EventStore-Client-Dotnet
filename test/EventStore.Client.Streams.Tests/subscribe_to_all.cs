using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class subscribe_to_all : IAsyncLifetime {
		private readonly Fixture _fixture;

		/// <summary>
		/// This class does not implement IClassFixture because it checks $all, and we want a fresh Node for each test.
		/// </summary>
		public subscribe_to_all(ITestOutputHelper outputHelper) {
			_fixture = new Fixture();
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task Callback_calls_subscription_dropped_when_disposed() {
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, false, SubscriptionDropped)
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
			var dropped = new TaskCompletionSource<Exception?>();
			
			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start);
			var testEvent = _fixture.CreateTestEvents(1).First();
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();
			
			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);
			
			// new event after subscription is disposed
			await _fixture.Client.AppendToStreamAsync($"test-{Guid.NewGuid()}", StreamState.NoStream, new[]{testEvent});

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

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, false, SubscriptionDropped)
				.WithTimeout();

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
			
			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start);
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
		public async Task Callback_subscribe_to_empty_database() {
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, false, SubscriptionDropped)
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
				if (!SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					appeared.TrySetResult(true);
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_subscribe_to_empty_database() {
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();
			
			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			Assert.False(appeared.Task.IsCompleted);

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				if (!SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					appeared.TrySetResult(true);
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		[Fact]
		public async Task Callback_reads_all_existing_events_and_keep_listening_to_new_ones() {
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
			var appearedEvents = new List<EventRecord>();
			var beforeEvents = _fixture.CreateTestEvents(10).ToArray();
			var afterEvents = _fixture.CreateTestEvents(10).ToArray();

			var allStreams = new List<string>();

			foreach (var @event in beforeEvents.Concat((afterEvents))) {
				allStreams.Add($"stream-{@event.EventId:n}");
			}
			
			foreach (var @event in beforeEvents) {
				await _fixture.Client.AppendToStreamAsync($"stream-{@event.EventId:n}", StreamState.NoStream,
					new[] {@event});
			}

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, false, SubscriptionDropped)
				.WithTimeout();

			foreach (var @event in afterEvents) {
				await _fixture.Client.AppendToStreamAsync($"stream-{@event.EventId:n}", StreamState.NoStream,
					new[] {@event});
			}

			await appeared.Task.WithTimeout();

			Assert.Equal(beforeEvents.Concat(afterEvents).Select(x => x.EventId),
				appearedEvents.Select(x => x.EventId));

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());
			}

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (allStreams.Contains(e.OriginalStreamId)) {
					appearedEvents.Add(e.Event);

					if (appearedEvents.Count >= beforeEvents.Length + afterEvents.Length) {
						appeared.TrySetResult(true);
					}
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
				dropped.SetResult((reason, ex));
		}
		
		[Fact]
		public async Task Iterator_reads_all_existing_events_and_keep_listening_to_new_ones() {
			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();
			var appearedEvents = new List<EventRecord>();
			var beforeEvents = _fixture.CreateTestEvents(10).ToArray();
			var afterEvents = _fixture.CreateTestEvents(10).ToArray();

			var allStreams = new List<string>();

			foreach (var @event in beforeEvents.Concat((afterEvents))) {
				allStreams.Add($"stream-{@event.EventId:n}");
			}
			
			foreach (var @event in beforeEvents) {
				await _fixture.Client.AppendToStreamAsync($"stream-{@event.EventId:n}", StreamState.NoStream,
					new[] {@event});
			}
			
			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			foreach (var @event in afterEvents) {
				await _fixture.Client.AppendToStreamAsync($"stream-{@event.EventId:n}", StreamState.NoStream,
					new[] { @event });
			}

			await appeared.Task.WithTimeout();

			Assert.Equal(beforeEvents.Concat(afterEvents).Select(x => x.EventId),
				appearedEvents.Select(x => x.EventId));

			if (dropped.Task.IsCompleted) {
				Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
			}

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				if (allStreams.Contains(e.OriginalStreamId)) {
					appearedEvents.Add(e.Event);

					if (appearedEvents.Count >= beforeEvents.Length + afterEvents.Length) {
						appeared.TrySetResult(true);
					}
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() =>
				Client.SetStreamMetadataAsync(SystemStreams.AllStream, StreamState.Any,
					new StreamMetadata(acl: new StreamAcl(SystemRoles.All)), userCredentials: TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}

		public Task InitializeAsync() => _fixture.InitializeAsync();
		public Task DisposeAsync() => _fixture.DisposeAsync();
		
		async void ReadMessages(EventStoreClient.SubscriptionResult subscription, Func<ResolvedEvent, Task> eventAppeared, Action<Exception?> subscriptionDropped) {
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
				subscriptionDropped(exception);
			} catch (Exception ex) {
				subscriptionDropped(ex);
			}
		}
	}
}
