using Grpc.Core;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_stream_with_timeout : IClassFixture<read_stream_with_timeout.Fixture> {
	readonly Fixture _fixture;

	public read_stream_with_timeout(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task fails_when_operation_expired() {
		var stream = _fixture.GetStreamName();

		await _fixture.Client.AppendToStreamAsync(stream, StreamRevision.None, _fixture.CreateTestEvents());

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => _fixture.Client
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

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}