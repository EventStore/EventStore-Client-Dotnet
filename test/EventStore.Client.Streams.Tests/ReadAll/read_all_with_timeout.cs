using Grpc.Core;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "LongRunning")]
[Trait("Category", "AllStream")]
[Trait("Category", "Read")]
public class read_all_with_timeout : IClassFixture<EventStoreFixture> {
	public read_all_with_timeout(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task fails_when_operation_expired() {
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams
				.ReadAllAsync(
					Direction.Backwards,
					Position.Start,
					1,
					false,
					TimeSpan.Zero
				)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}
}