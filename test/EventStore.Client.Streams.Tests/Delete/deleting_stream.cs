using Grpc.Core;

namespace EventStore.Client.Streams.Tests.Delete; 

[Trait("Category", "Operation:Delete")]
public class deleting_stream(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) { 
	public static IEnumerable<object?[]> ExpectedStreamStateCases() {
		yield return new object?[] { StreamState.Any, nameof(StreamState.Any) };
		yield return new object?[] { StreamState.NoStream, nameof(StreamState.NoStream) };
	}

	[Theory]
	[MemberData(nameof(ExpectedStreamStateCases))]
	public async Task hard_deleting_a_stream_that_does_not_exist_with_expected_version_does_not_throw(StreamState expectedVersion, string name) {
		var stream = $"{Fixture.GetStreamName()}_{name}";

		await Fixture.Streams.TombstoneAsync(stream, expectedVersion);
	}

	[Regression.Fact(21, "fixed by")]
	public async Task soft_deleting_a_stream_that_exists() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamRevision.None, Fixture.CreateTestEvents());

		await Fixture.Streams.DeleteAsync(stream, StreamState.StreamExists);
	}

	[Fact]
	public async Task hard_deleting_a_stream_that_does_not_exist_with_wrong_expected_version_throws() {
		var stream = Fixture.GetStreamName();

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.TombstoneAsync(stream, new StreamRevision(0)));
	}

	[Fact]
	public async Task soft_deleting_a_stream_that_does_not_exist_with_wrong_expected_version_throws() {
		var stream = Fixture.GetStreamName();

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.DeleteAsync(stream, new StreamRevision(0)));
	}

	[Fact]
	public async Task hard_deleting_a_stream_should_return_log_position() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		var deleteResult = await Fixture.Streams.TombstoneAsync(stream, writeResult.NextExpectedStreamRevision);

		Assert.True(deleteResult.LogPosition > writeResult.LogPosition);
	}

	[Fact]
	public async Task soft_deleting_a_stream_should_return_log_position() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		var deleteResult = await Fixture.Streams.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

		Assert.True(deleteResult.LogPosition > writeResult.LogPosition);
	}

	[Fact]
	public async Task hard_deleting_a_deleted_stream_should_throw() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Assert.ThrowsAsync<StreamDeletedException>(() => Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream));
	}
	
	
	[Fact]
	public async Task with_timeout_any_stream_revision_delete_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams.DeleteAsync(stream, StreamState.Any, TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task with_timeout_stream_revision_delete_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams.DeleteAsync(stream, new StreamRevision(0), TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task with_timeout_any_stream_revision_tombstoning_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams.TombstoneAsync(stream, StreamState.Any, TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task with_timeout_stream_revision_tombstoning_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams.TombstoneAsync(stream, new StreamRevision(0), TimeSpan.Zero)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}
}