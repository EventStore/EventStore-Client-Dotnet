namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Stream")]
[Trait("Category", "Append")]
public class append_to_stream_limits : IClassFixture<StreamLimitsFixture> {
	public append_to_stream_limits(ITestOutputHelper output, StreamLimitsFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	StreamLimitsFixture Fixture { get; }
	
	[Fact]
	public async Task succeeds_when_size_is_less_than_max_append_size() {
		var stream = Fixture.GetStreamName();
		
		var (events, size) = Fixture.CreateTestEventsUpToMaxSize(StreamLimitsFixture.MaxAppendSize - 1);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
	}

	[Fact]
	public async Task fails_when_size_exceeds_max_append_size() {
		var stream = Fixture.GetStreamName();

		var eventsAppendSize = StreamLimitsFixture.MaxAppendSize * 2;
		
		// beware of the size of the events...
		var (events, size) = Fixture.CreateTestEventsUpToMaxSize(eventsAppendSize);
		
		size.ShouldBeGreaterThan(StreamLimitsFixture.MaxAppendSize);
		
		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.NoStream, events)
			.ShouldThrowAsync<MaximumAppendSizeExceededException>();

		ex.MaxAppendSize.ShouldBe(StreamLimitsFixture.MaxAppendSize);
	}
}

public class StreamLimitsFixture() : EventStoreFixture(x => x.WithMaxAppendSize(MaxAppendSize)) {
	public const uint MaxAppendSize = 64;
	
	public (IEnumerable<EventData> Events, uint size) CreateTestEventsUpToMaxSize(uint maxSize) {
		var size   = 0;
		var events = new List<EventData>();
		
		foreach (var evt in CreateTestEvents(int.MaxValue)) {
			size += evt.Data.Length;
		
			if (size >= maxSize) {
				size -= evt.Data.Length;
				break;
			}

			events.Add(evt);
		}

		return (events, (uint)size);
	}
}