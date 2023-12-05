namespace EventStore.Client.Streams.Tests;

[Network]
public class read_stream_forward_messages : IClassFixture<EventStoreFixture> {
	public read_stream_forward_messages(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	int EventCount { get; } = 64;

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
		var events = Fixture.CreateTestEvents(EventCount).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			streamName,
			StreamPosition.Start
		).Messages.ToArrayAsync();

		Assert.Equal(
			EventCount + (Fixture.EventStoreHasLastStreamPosition ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(EventCount, result.OfType<StreamMessage.Event>().Count());
		var first = Assert.IsType<StreamMessage.Event>(result[1]);
		Assert.Equal(new(0), first.ResolvedEvent.OriginalEventNumber);
		var last = Assert.IsType<StreamMessage.Event>(result[Fixture.EventStoreHasLastStreamPosition ? ^2 : ^1]);
		Assert.Equal(new((ulong)EventCount - 1), last.ResolvedEvent.OriginalEventNumber);

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(
				new StreamMessage.LastStreamPosition(new((ulong)EventCount - 1)),
				result[^1]
			);
	}

	[Fact]
	public async Task stream_found_truncated() {
		var events = Fixture.CreateTestEvents(EventCount).ToArray();

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
			EventCount - 32 + (Fixture.EventStoreHasLastStreamPosition ? 3 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(new StreamMessage.FirstStreamPosition(new(32)), result[1]);

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(
				new StreamMessage.LastStreamPosition(new((ulong)EventCount - 1)),
				result[^1]
			);
	}
}