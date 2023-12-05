namespace EventStore.Client.Streams.Tests;

public class subscribe_resolve_link_to : IClassFixture<RunProjectionsTestFixture> {
	public subscribe_resolve_link_to(ITestOutputHelper output, RunProjectionsTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	RunProjectionsTestFixture Fixture { get; }

	[Fact]
	public async Task stream_subscription() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(20).ToArray();

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.AsEnumerable().GetEnumerator();

		enumerator.MoveNext();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
			.WithTimeout();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				$"$et-{EventStoreClientFixtureBase.TestEventType}",
				FromStream.Start,
				EventAppeared,
				true,
				SubscriptionDropped,
				TestCredentials.Root
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
			.WithTimeout();

		await appeared.Task.WithTimeout();

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			try {
				Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
				if (!enumerator.MoveNext())
					appeared.TrySetResult(true);
			}
			catch (Exception ex) {
				appeared.TrySetException(ex);
				throw;
			}

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task all_subscription() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(20).ToArray();

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.AsEnumerable().GetEnumerator();

		enumerator.MoveNext();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
			.WithTimeout();

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.Start,
				EventAppeared,
				true,
				SubscriptionDropped,
				userCredentials: TestCredentials.Root
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, afterEvents).WithTimeout();

		await appeared.Task.WithTimeout();

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (e.OriginalEvent.EventStreamId != $"$et-{EventStoreClientFixtureBase.TestEventType}")
				return Task.CompletedTask;

			try {
				Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
				if (!enumerator.MoveNext())
					appeared.TrySetResult(true);
			}
			catch (Exception ex) {
				appeared.TrySetException(ex);
				throw;
			}

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task all_filtered_subscription() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(20).ToArray();

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.AsEnumerable().GetEnumerator();

		enumerator.MoveNext();

		var result = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
			.WithTimeout();

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.Start,
				EventAppeared,
				true,
				SubscriptionDropped,
				new(StreamFilter.Prefix($"$et-{EventStoreClientFixtureBase.TestEventType}")),
				TestCredentials.Root
			)
			.WithTimeout();

		result = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, afterEvents)
			.WithTimeout();

		await appeared.Task.WithTimeout();

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (e.OriginalEvent.EventStreamId != $"$et-{EventStoreClientFixtureBase.TestEventType}")
				return Task.CompletedTask;

			try {
				Assert.Equal(enumerator.Current.EventId, e.Event.EventId);
				if (!enumerator.MoveNext())
					appeared.TrySetResult(true);
			}
			catch (Exception ex) {
				appeared.TrySetException(ex);
				throw;
			}

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}
}