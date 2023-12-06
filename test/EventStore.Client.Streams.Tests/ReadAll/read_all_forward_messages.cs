namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "AllStream")]
[Trait("Category", "Read")]
public class read_all_forward_messages : IClassFixture<EventStoreFixture> {
	public read_all_forward_messages(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task stream_found() {
		var events = Fixture.CreateTestEvents(32).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadAllAsync(
			Direction.Forwards,
			Position.Start,
			32,
			userCredentials: TestCredentials.Root
		).Messages.ToArrayAsync();

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
	}
}