using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	public class subscribe_resolve_link_to : IAsyncLifetime {
		private readonly Fixture _fixture;

		public subscribe_resolve_link_to(ITestOutputHelper outputHelper) {
			_fixture = new Fixture();
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task Callback_stream_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

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
				.SubscribeToStreamAsync(stream,
					FromStream.Start, EventAppeared, true, SubscriptionDropped,
					userCredentials: TestCredentials.Root)
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
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
		public async Task Iterator_stream_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			var subscription = _fixture.Client.SubscribeToStream(stream, FromStream.Start, true);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);
			
			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
				.WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				try {
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
		public async Task Callback_all_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, true, SubscriptionDropped, userCredentials: TestCredentials.Root)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents).WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (e.OriginalEvent.EventStreamId != stream) {
					return Task.CompletedTask;
				}

				try {
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
		public async Task Iterator_all_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start, true);
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents).WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				if (e.OriginalEvent.EventStreamId != stream) {
					return Task.CompletedTask;
				}

				try {
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
		public async Task Callback_all_filtered_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client.SubscribeToAllAsync(FromAll.Start,
					EventAppeared, true, SubscriptionDropped,
					new SubscriptionFilterOptions(
						StreamFilter.Prefix(stream)),
					userCredentials: TestCredentials.Root)
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
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
		public async Task Iterator_all_filtered_subscription() {
			var stream = $"{_fixture.GetStreamName()}{Guid.NewGuid()}";

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<Exception?>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			var subscription = _fixture.Client.SubscribeToAll(FromAll.Start, true, new SubscriptionFilterOptions(
				StreamFilter.Prefix(stream)));
			ReadMessages(subscription, EventAppeared, SubscriptionDropped);

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
				.WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var ex = await dropped.Task.WithTimeout();
			Assert.Null(ex);

			Task EventAppeared(ResolvedEvent e) {
				try {
					Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
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
			public Fixture() : base(env: new Dictionary<string, string> {
				["EVENTSTORE_RUN_PROJECTIONS"] = "All",
				["EVENTSTORE_START_STANDARD_PROJECTIONS"] = "True"
			}) {
			}

			protected override Task Given() => Task.CompletedTask;
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
