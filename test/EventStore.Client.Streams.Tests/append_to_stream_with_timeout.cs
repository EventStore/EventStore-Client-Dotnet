using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	#if !NETCOREAPP3_1_OR_GREATER
	[Trait("Category", "Network")]
	public class append_to_stream_with_timeout : IClassFixture<append_to_stream_with_timeout.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream_with_timeout(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task any_stream_revision_fails_when_operation_expired() {
			var stream = _fixture.GetStreamName();

			var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
				_fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents(100),
					options => options.TimeoutAfter = TimeSpan.FromTicks(1)));

			if (exception is RpcException rpcException) {
				Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
			} else if (exception is not TimeoutException) {
				throw new Exception($"thrown exception was not {nameof(TimeoutException)} or {nameof(RpcException)}");
			}
		}

		[Fact]
		public async Task stream_revision_fails_when_operation_expired() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents());

			var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
				_fixture.Client.AppendToStreamAsync(stream, new StreamRevision(0), _fixture.CreateTestEvents(100),
					options => options.TimeoutAfter = TimeSpan.FromTicks(1)));

			if (exception is RpcException rpcException) {
				Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
			} else if (exception is not TimeoutException) {
				throw new Exception($"thrown exception was not {nameof(TimeoutException)} or {nameof(RpcException)}");
			}
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
	#endif
}
