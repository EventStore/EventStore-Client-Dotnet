using Grpc.Core;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "StreamMetadata")]
public class stream_metadata_with_timeout : IClassFixture<EventStoreFixture> {
	public stream_metadata_with_timeout(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task set_with_any_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.SetStreamMetadataAsync(
					stream,
					StreamState.Any,
					new(),
					deadline: TimeSpan.Zero
				)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task set_with_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.SetStreamMetadataAsync(
					stream,
					new StreamRevision(0),
					new(),
					deadline: TimeSpan.Zero
				)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task get_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.GetStreamMetadataAsync(stream, TimeSpan.Zero)
		);
	}
}