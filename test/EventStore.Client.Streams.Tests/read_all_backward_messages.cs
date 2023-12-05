namespace EventStore.Client.Streams.Tests; 

[Network]
public class read_all_backward_messages : IClassFixture<EventStoreFixture> {
	public read_all_backward_messages(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task stream_found() {
		var events = Fixture.CreateTestEvents(32).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadAllAsync(
			Direction.Backwards,
			Position.End,
			32,
			userCredentials: TestCredentials.Root
		).Messages.ToArrayAsync();

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
	}
}