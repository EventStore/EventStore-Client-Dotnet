namespace EventStore.Client.Streams.Tests; 

[Network]
public class read_stream_forward : IClassFixture<EventStoreFixture> {
	public read_stream_forward(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Theory]
	[InlineData(0)]
	public async Task count_le_equal_zero_throws(long maxCount) {
		var stream = Fixture.GetStreamName();

		var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() =>
				Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, maxCount)
					.ToArrayAsync().AsTask()
		);

		Assert.Equal(nameof(maxCount), ex.ParamName);
	}

	[Fact]
	public async Task stream_does_not_exist_throws() {
		var stream = Fixture.GetStreamName();

		var ex = await Assert.ThrowsAsync<StreamNotFoundException>(
			() => Fixture.Streams
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(stream, ex.Stream);
	}

	[Fact]
	public async Task stream_does_not_exist_can_be_checked() {
		var stream = Fixture.GetStreamName();

		var result = Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1);

		var state = await result.ReadState;
		Assert.Equal(ReadState.StreamNotFound, state);
	}

	[Fact]
	public async Task stream_deleted_throws() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		var ex = await Assert.ThrowsAsync<StreamDeletedException>(
			() => Fixture.Streams
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(stream, ex.Stream);
	}

	[Theory]
	[InlineData("small_events", 10, 1)]
	[InlineData("large_events", 2, 1_000_000)]
	public async Task returns_events_in_order(string suffix, int count, int metadataSize) {
		var stream = $"{Fixture.GetStreamName()}_{suffix}";

		var expected = Fixture.CreateTestEvents(count, metadataSize: metadataSize).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		var actual = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, expected.Length)
			.Select(x => x.Event).ToArrayAsync();

		Assert.True(EventDataComparer.Equal(expected, actual));
	}

	[Fact]
	public async Task be_able_to_read_single_event_from_arbitrary_position() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(10).ToArray();

		var expected = events[7];

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, new(7), 1)
			.Select(x => x.Event)
			.SingleAsync();

		Assert.True(EventDataComparer.Equal(expected, actual));
	}

	[Fact]
	public async Task be_able_to_read_from_arbitrary_position() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, new(3), 2)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.True(EventDataComparer.Equal(events.Skip(3).Take(2).ToArray(), actual));
	}

	[Fact]
	public async Task be_able_to_read_first_event() {
		var stream = Fixture.GetStreamName();

		var testEvents = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, testEvents);

		var events = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Single(events);
		Assert.True(EventDataComparer.Equal(testEvents[0], events[0]));
	}

	[Fact]
	public async Task max_count_is_respected() {
		var        streamName = Fixture.GetStreamName();
		const int  count      = 20;
		const long maxCount   = count / 2;

		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			StreamState.NoStream,
			Fixture.CreateTestEvents(count)
		);

		var events = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start, maxCount)
			.Take(count)
			.ToArrayAsync();

		Assert.Equal(maxCount, events.Length);
	}

	[Fact]
	public async Task reads_all_events_by_default() {
		var       streamName = Fixture.GetStreamName();
		const int maxCount   = 200;
		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			StreamState.NoStream,
			Fixture.CreateTestEvents(maxCount)
		);

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start)
			.CountAsync();

		Assert.True(count == maxCount);
	}

	[Fact]
	public async Task populates_log_position_of_event() {
		if (EventStoreTestServer.Version.Major < 22)
			return;

		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(1).ToArray();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Single(actual);
		Assert.Equal(writeResult.LogPosition.PreparePosition, writeResult.LogPosition.CommitPosition);
		Assert.Equal(writeResult.LogPosition, actual.First().Position);
	}
}