namespace EventStore.Client.Tests;

[Trait("Category", "Target:Stream")]
[Trait("Category", "Operation:Read")]
[Trait("Category", "Operation:Read:Forwards")]
public class ReadStreamForwardTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : EventStorePermanentTests<KurrentPermanentFixture>(output, fixture) {
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

		var expected = Fixture.CreateTestEvents(count, metadata: Fixture.CreateMetadataOfSize(metadataSize)).ToArray();

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
		if (KurrentPermanentTestNode.Version.Major < 22)
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

	[Fact]
	public async Task stream_not_found() {
		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			Fixture.GetStreamName(),
			StreamPosition.Start
		).Messages.SingleAsync();

		Assert.Equal(StreamMessage.NotFound.Instance, result);
	}

	[Fact]
	public async Task stream_found() {
		const int eventCount = 64;

		var events = Fixture.CreateTestEvents(eventCount).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			streamName,
			StreamPosition.Start
		).Messages.ToArrayAsync();

		Assert.Equal(
			eventCount + (Fixture.EventStoreHasLastStreamPosition ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(eventCount, result.OfType<StreamMessage.Event>().Count());
		var first = Assert.IsType<StreamMessage.Event>(result[1]);
		Assert.Equal(new(0), first.ResolvedEvent.OriginalEventNumber);
		var last = Assert.IsType<StreamMessage.Event>(result[Fixture.EventStoreHasLastStreamPosition ? ^2 : ^1]);
		Assert.Equal(new((ulong)eventCount - 1), last.ResolvedEvent.OriginalEventNumber);

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(
				new StreamMessage.LastStreamPosition(new((ulong)eventCount - 1)),
				result[^1]
			);
	}

	[Fact]
	public async Task stream_found_truncated() {
		const int eventCount = 64;

		var events = Fixture.CreateTestEvents(eventCount).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		await Fixture.Streams.SetStreamMetadataAsync(
			streamName,
			StreamState.Any,
			new(truncateBefore: new StreamPosition(32))
		);

		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			streamName,
			StreamPosition.Start
		).Messages.ToArrayAsync();

		Assert.Equal(
			eventCount - 32 + (Fixture.EventStoreHasLastStreamPosition ? 3 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(new StreamMessage.FirstStreamPosition(new(32)), result[1]);

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(
				new StreamMessage.LastStreamPosition(new((ulong)eventCount - 1)),
				result[^1]
			);
	}
}
