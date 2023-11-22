using Grpc.Core;

namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_all_with_timeout : IClassFixture<read_all_with_timeout.Fixture> {
	readonly Fixture _fixture;

	public read_all_with_timeout(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task fails_when_operation_expired() {
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => _fixture.Client
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

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}