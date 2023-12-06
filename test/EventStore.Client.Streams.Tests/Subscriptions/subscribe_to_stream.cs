namespace EventStore.Client.Streams.Tests;

[Trait("Category", "LongRunning")]
[Trait("Category", "Subscriptions")]
[Trait("Category", "Stream")]
public class subscribe_to_stream : IClassFixture<SubscriptionsFixture> {
	public subscribe_to_stream(ITestOutputHelper output, SubscriptionsFixture fixture) => Fixture = fixture.With(x => x.CaptureTestRun(output));

	SubscriptionsFixture Fixture { get; }

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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task subscribe_to_non_existing_stream_then_get_event() {
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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		Assert.True(await appeared.Task.WithTimeout());

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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task allow_multiple_subscriptions_to_same_stream() {
		var stream = Fixture.GetStreamName();

		var appeared = new TaskCompletionSource<bool>();

		var appearedCount = 0;

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		using var s1 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared).WithTimeout();

		using var s2 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.Start, EventAppeared).WithTimeout();

		Assert.True(await appeared.Task.WithTimeout());

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task calls_subscription_dropped_when_error_processing_event() {
		var stream            = Fixture.GetStreamName();
		var dropped           = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		var expectedException = new Exception("Error");

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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.SubscriberError, reason);
		Assert.Same(expectedException, ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) =>
			Task.FromException(expectedException);

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, beforeEvents)
			.WithTimeout();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.Start,
				EventAppeared,
				false,
				SubscriptionDropped
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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task catches_deletions() {
		var stream = Fixture.GetStreamName();

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var _ = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.Start,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);
		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var sdex = Assert.IsType<StreamDeletedException>(ex);
		Assert.Equal(stream, sdex.Stream);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}

	[Fact]
	public async Task with_revision_subscribe_to_non_existing_stream() {
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
	public async Task with_revision_subscribe_to_non_existing_stream_then_get_event() {
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
	public async Task with_revision_allow_multiple_subscriptions_to_same_stream() {
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
	public async Task with_revision_calls_subscription_dropped_when_disposed() {
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
	public async Task with_revision_calls_subscription_dropped_when_error_processing_event() {
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
	public async Task with_revision_reads_all_existing_events_and_keep_listening_to_new_ones() {
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
	
	[Fact]
	public async Task receives_all_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));
		
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync($"$et-{EventStoreFixture.TestEventType}", FromStream.Start, OnReceived, true, OnDropped)
			.WithTimeout();;
		
		await receivedAllEvents.Task.WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());
		
		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			var hasResolvedLink = re.OriginalEvent.EventStreamId.StartsWith($"$et-{EventStoreFixture.TestEventType}");
			if (availableEvents.RemoveWhere(x => x == re.Event.EventId && hasResolvedLink) == 0) {
				Fixture.Log.Debug("Received unexpected event {EventId} from stream {StreamId}", re.Event.EventId, re.OriginalEvent.EventStreamId);
				return Task.CompletedTask;
			}
			
			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}
			
			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) =>
			subscriptionDropped.SetResult(new(reason, ex));
	}
	
	[Fact]
	public async Task live_does_not_read_existing_events_but_keep_listening_to_new_ones() {
		var stream   = Fixture.GetStreamName();
		var appeared = new TaskCompletionSource<StreamPosition>();
		var dropped  = new TaskCompletionSource<bool>();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		using var _ = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				(_, e, _) => {
					appeared.TrySetResult(e.OriginalEventNumber);
					return Task.CompletedTask;
				},
				false,
				(s, reason, ex) => dropped.TrySetResult(true)
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			new StreamRevision(0),
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(1), await appeared.Task.WithTimeout());
	}

	[Fact]
	public async Task live_subscribe_to_non_existing_stream_and_then_catch_new_event() {
		var stream   = Fixture.GetStreamName();
		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<bool>();

		using var _ = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				(_, _, _) => {
					appeared.TrySetResult(true);
					return Task.CompletedTask;
				},
				false,
				(s, reason, ex) => dropped.TrySetResult(true)
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.True(await appeared.Task.WithTimeout());
	}

	[Fact]
	public async Task live_allow_multiple_subscriptions_to_same_stream() {
		var stream = Fixture.GetStreamName();

		var appeared = new TaskCompletionSource<bool>();

		var appearedCount = 0;

		using var s1 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
			.WithTimeout();

		using var s2 = await Fixture.Streams
			.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		Assert.True(await appeared.Task.WithTimeout());

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (++appearedCount == 2)
				appeared.TrySetResult(true);

			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task live_calls_subscription_dropped_when_disposed() {
		var stream = Fixture.GetStreamName();

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var _ = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			s.Dispose();
			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Null(ex);
		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
	}

	[Fact]
	public async Task live_catches_deletions() {
		var stream = Fixture.GetStreamName();

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var _ = await Fixture.Streams
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);
		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var sdex = Assert.IsType<StreamDeletedException>(ex);
		Assert.Equal(stream, sdex.Stream);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}
}