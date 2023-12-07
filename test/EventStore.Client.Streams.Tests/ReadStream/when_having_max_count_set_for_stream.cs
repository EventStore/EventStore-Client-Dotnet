namespace EventStore.Client.Streams.Tests;

[Trait("Category", "LongRunning")]
[Trait("Category", "Stream")]
[Trait("Category", "Read")]
public class when_having_max_count_set_for_stream (ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	[Fact]
	public async Task read_stream_forwards_respects_max_count() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(3, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(2).ToArray(), actual));
	}

	[Fact]
	public async Task read_stream_backwards_respects_max_count() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(3, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(2).Reverse().ToArray(), actual));
	}

	[Fact]
	public async Task after_setting_less_strict_max_count_read_stream_forward_reads_more_events() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		await Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(0), new(4));

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(4, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(1).ToArray(), actual));
	}

	[Fact]
	public async Task after_setting_more_strict_max_count_read_stream_forward_reads_less_events() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		await Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(0), new(2));

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(2, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(3).ToArray(), actual));
	}

	[Fact]
	public async Task after_setting_less_strict_max_count_read_stream_backwards_reads_more_events() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		await Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(0), new(4));

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(4, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(1).Reverse().ToArray(), actual));
	}

	[Fact]
	public async Task after_setting_more_strict_max_count_read_stream_backwards_reads_less_events() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new(3));

		var expected = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		await Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(0), new(2));

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 100)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(2, actual.Length);
		Assert.True(EventDataComparer.Equal(expected.Skip(3).Reverse().ToArray(), actual));
	}
}