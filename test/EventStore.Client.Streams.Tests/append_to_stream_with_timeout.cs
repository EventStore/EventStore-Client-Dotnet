using System;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class append_to_stream_with_timeout : IClassFixture<append_to_stream_with_timeout.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream_with_timeout(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task any_stream_revision_fails_when_operation_expired() {
			var stream = _fixture.GetStreamName();

			var ex = await Assert.ThrowsAsync<RpcException>(() =>
				_fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents(100),
					deadline: TimeSpan.FromTicks(1)));

			Assert.Equal(StatusCode.DeadlineExceeded, ex.StatusCode);
		}

		[Fact]
		public async Task stream_revision_fails_when_operation_expired() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents());

			var ex = await Assert.ThrowsAsync<RpcException>(() =>
				_fixture.Client.AppendToStreamAsync(stream, new StreamRevision(0), _fixture.CreateTestEvents(100),
					deadline: TimeSpan.Zero));

			Assert.Equal(StatusCode.DeadlineExceeded, ex.StatusCode);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
