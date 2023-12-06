namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "Stream")]
[Trait("Category", "Read")]
public class read_stream_backward_messages : IClassFixture<EventStoreFixture> {
	public read_stream_backward_messages(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	int EventCount { get; } = 32;

	[Fact]
	public async Task stream_not_found() {
		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Backwards,
			Fixture.GetStreamName(),
			StreamPosition.End
		).Messages.SingleAsync();

		Assert.Equal(StreamMessage.NotFound.Instance, result);
	}

	[Fact]
	public async Task stream_found() {
		var events = Fixture.CreateTestEvents(EventCount).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Backwards,
			streamName,
			StreamPosition.End
		).Messages.ToArrayAsync();

		Assert.Equal(
			EventCount + (Fixture.EventStoreHasLastStreamPosition ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(EventCount, result.OfType<StreamMessage.Event>().Count());

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(new StreamMessage.LastStreamPosition(new(31)), result[^1]);
	}
}