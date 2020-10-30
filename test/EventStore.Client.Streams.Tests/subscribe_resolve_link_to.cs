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
		public async Task stream_subscription() {
			var stream = _fixture.GetStreamName();

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client
				.SubscribeToStreamAsync($"$et-{EventStoreClientFixtureBase.TestEventType}", EventAppeared, true,
					SubscriptionDropped, userCredentials: TestCredentials.Root)
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

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception ex) =>
				dropped.SetResult((reason, ex));
		}

		[Fact]
		public async Task all_subscription() {
			var stream = _fixture.GetStreamName();

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client
				.SubscribeToAllAsync(EventAppeared, true, SubscriptionDropped, userCredentials: TestCredentials.Root)
				.WithTimeout();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
				.WithTimeout();

			await appeared.Task.WithTimeout();

			subscription.Dispose();

			var (reason, ex) = await dropped.Task.WithTimeout();

			Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
			Assert.Null(ex);

			Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (e.OriginalEvent.EventStreamId != $"$et-{EventStoreClientFixtureBase.TestEventType}") {
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

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception ex) =>
				dropped.SetResult((reason, ex));
		}

		[Fact]
		public async Task all_filtered_subscription() {
			var stream = _fixture.GetStreamName();

			var events = _fixture.CreateTestEvents(20).ToArray();

			var appeared = new TaskCompletionSource<bool>();
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();

			var beforeEvents = events.Take(10);
			var afterEvents = events.Skip(10);

			using var enumerator = events.AsEnumerable().GetEnumerator();

			enumerator.MoveNext();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
				.WithTimeout();

			using var subscription = await _fixture.Client.SubscribeToAllAsync(EventAppeared, true, SubscriptionDropped,
					new SubscriptionFilterOptions(StreamFilter.Prefix($"$et-{EventStoreClientFixtureBase.TestEventType}")),
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
				if (e.OriginalEvent.EventStreamId != $"$et-{EventStoreClientFixtureBase.TestEventType}") {
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

			void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception ex) =>
				dropped.SetResult((reason, ex));
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
	}
}
