namespace EventStore.Client.Streams.Tests; 

public class EventDataTests {
	[Fact]
	public void EmptyEventIdThrows() {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(
			() =>
				new EventData(Uuid.Empty, "-", Array.Empty<byte>())
		);

		Assert.Equal("eventId", ex.ParamName);
	}

	[Fact]
	public void MalformedContentTypeThrows() =>
		Assert.Throws<FormatException>(() => new EventData(Uuid.NewUuid(), "-", Array.Empty<byte>(), contentType: "application"));

	[Fact]
	public void InvalidContentTypeThrows() {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new EventData(Uuid.NewUuid(), "-", Array.Empty<byte>(), contentType: "application/xml"));
		Assert.Equal("contentType", ex.ParamName);
	}
}