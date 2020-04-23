using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class read_stream_with_timeout : IClassFixture<read_stream_with_timeout.Fixture> {
		private readonly Fixture _fixture;

		public read_stream_with_timeout(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task fails_when_operation_expired() {
			var stream = _fixture.GetStreamName();

			var rpcException = await Assert.ThrowsAsync<RpcException>(() => _fixture.Client
				.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1,
					options => options.TimeoutAfter = TimeSpan.Zero)
				.ToArrayAsync().AsTask());
			
			Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
