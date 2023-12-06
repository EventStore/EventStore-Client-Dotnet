using Grpc.Core;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
[Trait("Category", "Stream")]
[Trait("Category", "Read")]
public class read_stream_with_timeout : IClassFixture<EventStoreFixture> {
	public read_stream_with_timeout(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamRevision.None, Fixture.CreateTestEvents());

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams
				.ReadStreamAsync(
					Direction.Backwards,
					stream,
					StreamPosition.End,
					1,
					false,
					TimeSpan.Zero
				)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}
}