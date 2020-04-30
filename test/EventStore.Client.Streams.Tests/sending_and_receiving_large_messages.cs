using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	public class sending_and_receiving_large_messages : IClassFixture<sending_and_receiving_large_messages.Fixture> {
		private readonly Fixture _fixture;

		public sending_and_receiving_large_messages(Fixture fixture, ITestOutputHelper outputHelper) {
			_fixture = fixture;
			_fixture.CaptureLogs(outputHelper);
		}

		[Fact]
		public async Task over_the_hard_limit() {
			var streamName = _fixture.GetStreamName();
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Client.AppendToStreamAsync(
				streamName, StreamState.NoStream,
				_fixture.LargeEvent));
			var rpcEx = Assert.IsType<RpcException>(ex.InnerException);
			Assert.Equal(StatusCode.ResourceExhausted, rpcEx.StatusCode);
		}

		public class Fixture : EventStoreClientFixture {
			private const int MaximumSize = 16 * 1024 * 1024 - 10000; // magic number

			public Fixture() : base(env: new Dictionary<string, string> {
				["EVENTSTORE_MAX_APPEND_SIZE"] = $"{MaximumSize}"
			}) {

			}

			public IEnumerable<EventData> LargeEvent => CreateTestEvents()
				.Select(e => new EventData(e.EventId, "-", new byte[MaximumSize + 1]));

			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
