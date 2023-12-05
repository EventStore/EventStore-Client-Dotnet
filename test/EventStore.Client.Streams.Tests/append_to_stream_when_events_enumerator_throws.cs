namespace EventStore.Client.Streams.Tests; 

public class append_to_stream_when_events_enumerator_throws : IClassFixture<EventStoreFixture> {
	public append_to_stream_when_events_enumerator_throws(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }
	
	[Fact]
	public async Task the_write_does_not_succeed() {
		var streamName = Fixture.GetStreamName();

		await Fixture.Streams
			.AppendToStreamAsync(streamName, StreamRevision.None, GetEvents())
			.ShouldThrowAsync<EnumerationFailedException>();

		var result = Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);

		var state = await result.ReadState;

		state.ShouldBe(ReadState.StreamNotFound);
		
		return;

		IEnumerable<EventData> GetEvents() {
			var i = 0;
			foreach (var evt in Fixture.CreateTestEvents(5)) {
				if (i++ % 3 == 0)
					throw new EnumerationFailedException();

				yield return evt;
			}
		}
	}

	class EnumerationFailedException : Exception { }
}