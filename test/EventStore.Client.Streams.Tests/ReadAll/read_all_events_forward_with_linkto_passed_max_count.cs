using System.Text;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "LongRunning")]
[Trait("Category", "AllStream")]
[Trait("Category", "Read")]
public class read_all_events_forward_with_linkto_passed_max_count : IClassFixture<EventStoreFixture> {
	public read_all_events_forward_with_linkto_passed_max_count(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }
	
	[Fact]
	public async Task one_event_is_read() {
		const string deletedStream = nameof(deletedStream);
		const string linkedStream  = nameof(linkedStream);
	
		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.SetStreamMetadataAsync(
			deletedStream,
			StreamState.Any,
			new(2)
		);

		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.AppendToStreamAsync(
			linkedStream,
			StreamState.Any,
			new[] {
				new EventData(
					Uuid.NewUuid(),
					SystemEventTypes.LinkTo,
					Encoding.UTF8.GetBytes("0@" + deletedStream),
					Array.Empty<byte>(),
					Constants.Metadata.ContentTypes.ApplicationOctetStream
				)
			}
		);

		var events = await Fixture.Streams.ReadStreamAsync(
				Direction.Forwards,
				linkedStream,
				StreamPosition.Start,
				resolveLinkTos: true
			)
			.ToArrayAsync();
		
		Assert.Single(events);
	}
}