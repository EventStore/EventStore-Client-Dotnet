namespace EventStore.Client.Streams.Tests;

[DedicatedDatabase, LongRunning]
public class subscribe_to_all_with_position : IClassFixture<EventStoreFixture> {
	public subscribe_to_all_with_position(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task reads_all_existing_events_after_position_and_keep_listening_to_new_ones() {
		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.NoStream,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		var events = Fixture.CreateTestEvents(20).ToArray();

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.AsEnumerable().GetEnumerator();

		enumerator.MoveNext();

		var position = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.Select(x => x.OriginalEvent.Position)
			.FirstAsync();

		foreach (var @event in beforeEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{@event.EventId:n}",
				StreamState.NoStream,
				new[] { @event }
			);

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.After(position),
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		foreach (var @event in afterEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{@event.EventId:n}",
				StreamState.NoStream,
				new[] { @event }
			);

		await appeared.Task.WithTimeout();

		Assert.False(dropped.Task.IsCompleted);

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (position >= e.OriginalEvent.Position)
				appeared.TrySetException(new Exception());

			if (!SystemStreams.IsSystemStream(e.OriginalStreamId))
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
}

[DedicatedDatabase, LongRunning]
public class subscribe_to_all_with_position_to_empty_database : IClassFixture<EventStoreFixture> {
	
	public subscribe_to_all_with_position_to_empty_database(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task verified() {
		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.NoStream,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var firstEvent = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.FirstOrDefaultAsync();

		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(
				FromAll.After(firstEvent.OriginalEvent.Position),
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
			if (e.OriginalEvent.Position == firstEvent.OriginalEvent.Position) {
				appeared.TrySetException(new Exception());
				return Task.CompletedTask;
			}

			if (!SystemStreams.IsSystemStream(e.OriginalStreamId))
				appeared.TrySetResult(true);

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}
}

[DedicatedDatabase, LongRunning]
public class subscribe_to_all_with_position_calls_subscription_dropped_when_disposed : IClassFixture<EventStoreFixture> {
	public subscribe_to_all_with_position_calls_subscription_dropped_when_disposed(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task verified() {
		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.NoStream,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var firstEvent = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.FirstOrDefaultAsync();

		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(
				FromAll.After(firstEvent.OriginalEvent.Position),
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
}

[DedicatedDatabase, LongRunning]
public class subscribe_to_all_with_position_calls_subscription_dropped_when_error_processing_event : IClassFixture<EventStoreFixture> {
	public subscribe_to_all_with_position_calls_subscription_dropped_when_error_processing_event(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task verified() {
		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.NoStream,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		var stream  = Fixture.GetStreamName();
		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var expectedException = new Exception("Error");

		var firstEvent = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.FirstOrDefaultAsync();

		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(
				FromAll.After(firstEvent.OriginalEvent.Position),
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.SubscriberError, reason);
		Assert.Same(expectedException, ex);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) =>
			Task.FromException(expectedException);

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}
}