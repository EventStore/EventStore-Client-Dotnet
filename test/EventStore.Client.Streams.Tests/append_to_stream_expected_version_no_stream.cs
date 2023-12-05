namespace EventStore.Client.Streams.Tests;

public class append_to_stream_expected_version_no_stream : IClassFixture<EventStoreFixture> {
	public append_to_stream_expected_version_no_stream(ITestOutputHelper output, EventStoreFixture fixture) {
		Fixture = fixture.With(x => x.CaptureTestRun(output));
	}

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task succeeds() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), result!.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task returns_position() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.True(result.LogPosition > Position.Start);
	}
}