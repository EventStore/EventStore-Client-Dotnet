namespace EventStore.Client.Streams.Tests; 

[LongRunning]
public class subscribe_to_stream_live : IClassFixture<EventStoreFixture> {
	public subscribe_to_stream_live(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task does_not_read_existing_events_but_keep_listening_to_new_ones() {
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
	public async Task subscribe_to_non_existing_stream_and_then_catch_new_event() {
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
	public async Task allow_multiple_subscriptions_to_same_stream() {
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
	public async Task calls_subscription_dropped_when_disposed() {
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
	public async Task catches_deletions() {
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