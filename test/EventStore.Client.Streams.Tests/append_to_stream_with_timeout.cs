using Grpc.Core;

namespace EventStore.Client.Streams.Tests;

[Network]
public class append_to_stream_with_timeout : IClassFixture<EventStoreFixture> {
	public append_to_stream_with_timeout(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
		yield return new object?[] { StreamState.Any };
		yield return new object?[] { ulong.MaxValue - 1UL };
	}
	
	[Fact]
	public async Task any_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents(100),
			deadline: TimeSpan.FromTicks(1)
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	[Fact]
	public async Task stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents());
		
		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			new StreamRevision(0),
			Fixture.CreateTestEvents(10),
			deadline: TimeSpan.Zero
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}
}