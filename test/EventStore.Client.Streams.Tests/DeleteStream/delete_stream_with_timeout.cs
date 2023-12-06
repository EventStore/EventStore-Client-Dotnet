using Grpc.Core;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
[Trait("Category", "Delete")]
public class deleting_stream_with_timeout : IClassFixture<EventStoreFixture> {
	public deleting_stream_with_timeout(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task any_stream_revision_delete_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.DeleteAsync(stream, StreamState.Any, TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task stream_revision_delete_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.DeleteAsync(stream, new StreamRevision(0), TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task any_stream_revision_tombstoning_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.TombstoneAsync(stream, StreamState.Any, TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task stream_revision_tombstoning_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.TombstoneAsync(stream, new StreamRevision(0), TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}
}