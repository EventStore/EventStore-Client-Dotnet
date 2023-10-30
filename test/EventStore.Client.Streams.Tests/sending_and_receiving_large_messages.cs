using Grpc.Core;

namespace EventStore.Client.Streams.Tests; 

public class sending_and_receiving_large_messages : IClassFixture<sending_and_receiving_large_messages.Fixture> {
	readonly Fixture _fixture;

	public sending_and_receiving_large_messages(Fixture fixture, ITestOutputHelper outputHelper) {
		_fixture = fixture;
		_fixture.CaptureLogs(outputHelper);
	}

	[Fact]
	public async Task over_the_hard_limit() {
		var streamName = _fixture.GetStreamName();
		var ex = await Assert.ThrowsAsync<RpcException>(
			() => _fixture.Client.AppendToStreamAsync(
				streamName,
				StreamState.NoStream,
				_fixture.LargeEvent
			)
		);

		Assert.Equal(StatusCode.ResourceExhausted, ex.StatusCode);
	}

	public class Fixture : EventStoreClientFixture {
		const int MaximumSize = 16 * 1024 * 1024 - 10000; // magic number

		public Fixture() : base(
			env: new() {
				["EVENTSTORE_MAX_APPEND_SIZE"] = $"{MaximumSize}"
			}
		) { }

		public IEnumerable<EventData> LargeEvent =>
			CreateTestEvents()
				.Select(e => new EventData(e.EventId, "-", new byte[MaximumSize + 1]));

		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}