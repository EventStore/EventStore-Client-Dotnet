namespace EventStore.Client.Streams.Tests; 

[DedicatedDatabase, LongRunning]
public class subscribe_to_stream_with_revision : IClassFixture<EventStoreFixture> {
	public subscribe_to_stream_with_revision(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task subscribe_to_non_existing_stream() {
		var stream   = Fixture.GetStreamName();
		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.Start,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		Assert.False(appeared.Task.IsCompleted);

		if (dropped.Task.IsCompleted)
			Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			appeared.TrySetResult(true);
			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task subscribe_to_non_existing_stream_then_get_event() {
		var stream   = Fixture.GetStreamName();
		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.After(StreamPosition.Start),
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(2)
		);

		Assert.True(await appeared.Task.WithTimeout());

		if (dropped.Task.IsCompleted)
			Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (e.OriginalEvent.EventNumber == StreamPosition.Start)
				appeared.TrySetException(new Exception());
			else
				appeared.TrySetResult(true);

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task allow_multiple_subscriptions_to_same_stream() {
		var stream = Fixture.GetStreamName();

		var appeared = new TaskCompletionSource<bool>();

		var appearedCount = 0;

		using var s1 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.After(StreamPosition.Start), EventAppeared)
			.WithTimeout();

		using var s2 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.After(StreamPosition.Start), EventAppeared)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		Assert.True(await appeared.Task.WithTimeout());

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (e.OriginalEvent.EventNumber == StreamPosition.Start) {
				appeared.TrySetException(new Exception());
				return Task.CompletedTask;
			}

			if (++appearedCount == 2)
				appeared.TrySetResult(true);

			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task calls_subscription_dropped_when_disposed() {
		var stream  = Fixture.GetStreamName();
		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.Start,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		if (dropped.Task.IsCompleted)
			Assert.False(dropped.Task.IsCompleted, dropped.Task.Result.ToString());

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task calls_subscription_dropped_when_error_processing_event() {
		var stream            = Fixture.GetStreamName();
		var dropped           = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		var expectedException = new Exception("Error");

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		using var subscription = await Fixture.Streams.SubscribeToStreamAsync(
				stream,
				FromStream.Start,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.SubscriberError, reason);
		Assert.Same(expectedException, ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.FromException(expectedException);

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task reads_all_existing_events_and_keep_listening_to_new_ones() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(20).ToArray();

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.AsEnumerable().GetEnumerator();

		enumerator.MoveNext();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, beforeEvents);

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.After(StreamPosition.Start),
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, afterEvents);

		await appeared.Task.WithTimeout();

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			try {
				Assert.Equal(enumerator.Current.EventId, e.OriginalEvent.EventId);
				if (!enumerator.MoveNext())
					appeared.TrySetResult(true);
			}
			catch (Exception ex) {
				appeared.TrySetException(ex);
				throw;
			}

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}
}