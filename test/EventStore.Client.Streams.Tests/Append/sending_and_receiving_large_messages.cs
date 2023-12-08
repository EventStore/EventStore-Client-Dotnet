using Grpc.Core;

namespace EventStore.Client.Streams.Tests.Append;

[Trait("Category", "Target:Stream")]
[Trait("Category", "Operation:Append")]
public class sending_and_receiving_large_messages(ITestOutputHelper output, sending_and_receiving_large_messages.CustomFixture fixture)
	: EventStoreTests<sending_and_receiving_large_messages.CustomFixture>(output, fixture) {
	[Fact]
	public async Task over_the_hard_limit() {
		var streamName = Fixture.GetStreamName();
		var largeEvent = Fixture.CreateTestEvents()
			.Select(e => new EventData(e.EventId, "-", new byte[CustomFixture.MaximumSize + 1]));

		var ex = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams.AppendToStreamAsync(
				streamName,
				StreamState.NoStream,
				largeEvent
			)
		);

		Assert.Equal(StatusCode.ResourceExhausted, ex.StatusCode);
	}

	public class CustomFixture() : EventStoreFixture(x => x.WithMaxAppendSize(MaximumSize)) {
		public const int MaximumSize = 16 * 1024 * 1024 - 10000;
	}
}