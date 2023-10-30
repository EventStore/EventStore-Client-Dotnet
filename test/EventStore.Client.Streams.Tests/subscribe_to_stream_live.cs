namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "LongRunning")]
public class subscribe_to_stream_live : IClassFixture<subscribe_to_stream_live.Fixture> {
	readonly Fixture _fixture;

	public subscribe_to_stream_live(Fixture fixture, ITestOutputHelper outputHelper) {
		_fixture = fixture;
		_fixture.CaptureLogs(outputHelper);
	}

	[Fact]
	public async Task does_not_read_existing_events_but_keep_listening_to_new_ones() {
		var stream   = _fixture.GetStreamName();
		var appeared = new TaskCompletionSource<StreamPosition>();
		var dropped  = new TaskCompletionSource<bool>();

		await _fixture.Client.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			_fixture.CreateTestEvents()
		);

		using var _ = await _fixture.Client
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

		await _fixture.Client.AppendToStreamAsync(
			stream,
			new StreamRevision(0),
			_fixture.CreateTestEvents()
		);

		Assert.Equal(new(1), await appeared.Task.WithTimeout());
	}

	[Fact]
	public async Task subscribe_to_non_existing_stream_and_then_catch_new_event() {
		var stream   = _fixture.GetStreamName();
		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<bool>();

		using var _ = await _fixture.Client
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

		await _fixture.Client.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			_fixture.CreateTestEvents()
		);

		Assert.True(await appeared.Task.WithTimeout());
	}

	[Fact]
	public async Task allow_multiple_subscriptions_to_same_stream() {
		var stream = _fixture.GetStreamName();

		var appeared = new TaskCompletionSource<bool>();

		var appearedCount = 0;

		using var s1 = await _fixture.Client
			.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
			.WithTimeout();

		using var s2 = await _fixture.Client
			.SubscribeToStreamAsync(stream, FromStream.End, EventAppeared)
			.WithTimeout();

		await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

		Assert.True(await appeared.Task.WithTimeout());

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
			if (++appearedCount == 2)
				appeared.TrySetResult(true);

			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task calls_subscription_dropped_when_disposed() {
		var stream = _fixture.GetStreamName();

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var _ = await _fixture.Client
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

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
		var stream = _fixture.GetStreamName();

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		using var _ = await _fixture.Client
			.SubscribeToStreamAsync(
				stream,
				FromStream.End,
				EventAppeared,
				false,
				SubscriptionDropped
			)
			.WithTimeout();

		await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);
		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var sdex = Assert.IsType<StreamDeletedException>(ex);
		Assert.Equal(stream, sdex.Stream);

		Task EventAppeared(StreamSubscription s, ResolvedEvent e, CancellationToken ct) => Task.CompletedTask;

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) => dropped.SetResult((reason, ex));
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}