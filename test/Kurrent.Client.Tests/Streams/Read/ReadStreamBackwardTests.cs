using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Grpc.Core;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Read")]
[Trait("Category", "Operation:Read:Backwards")]
public class ReadStreamBackwardTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[Theory]
	[InlineData(0)]
	public async Task count_le_equal_zero_throws(long maxCount) {
		var stream = Fixture.GetStreamName();

		var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() =>
				Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.Start, maxCount)
					.ToArrayAsync().AsTask()
		);

		Assert.Equal(nameof(maxCount), ex.ParamName);
	}

	[Fact]
	public async Task stream_does_not_exist_throws() {
		var stream = Fixture.GetStreamName();

		var ex = await Assert.ThrowsAsync<StreamNotFoundException>(
			() => Fixture.Streams
				.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(stream, ex.Stream);
	}

	[Fact]
	public async Task stream_does_not_exist_can_be_checked() {
		var stream = Fixture.GetStreamName();

		var result = Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1);

		var state = await result.ReadState;
		Assert.Equal(ReadState.StreamNotFound, state);
	}

	[Fact]
	public async Task stream_deleted_throws() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		var ex = await Assert.ThrowsAsync<StreamDeletedException>(
			() => Fixture.Streams
				.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(stream, ex.Stream);
	}

	[Theory]
	[InlineData("small_events", 10, 1)]
	[InlineData("large_events", 2, 1_000_000)]
	public async Task returns_events_in_reversed_order(string suffix, int count, int metadataSize) {
		var stream = $"{Fixture.GetStreamName()}_{suffix}";

		var expected = Fixture.CreateTestEvents(count, metadata: Fixture.CreateMetadataOfSize(metadataSize)).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		var actual = await Fixture.Streams
			.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, expected.Length)
			.Select(x => x.Event).ToArrayAsync();

		Assert.True(
			EventDataComparer.Equal(
				Enumerable.Reverse(expected).ToArray(),
				actual
			)
		);
	}

	[Fact]
	public async Task be_able_to_read_single_event_from_arbitrary_position() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(10).ToArray();

		var expected = events[7];

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, new(7), 1)
			.Select(x => x.Event)
			.SingleAsync();

		Assert.True(EventDataComparer.Equal(expected, actual));
	}

	[Fact]
	public async Task be_able_to_read_from_arbitrary_position() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, new(3), 2)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.True(EventDataComparer.Equal(events.Skip(2).Take(2).Reverse().ToArray(), actual));
	}

	[Fact]
	public async Task be_able_to_read_first_event() {
		var stream = Fixture.GetStreamName();

		var testEvents = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, testEvents);

		var events = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.Start, 1)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Single(events);
		Assert.True(EventDataComparer.Equal(testEvents[0], events[0]));
	}

	[Fact]
	public async Task be_able_to_read_last_event() {
		var stream     = Fixture.GetStreamName();
		var testEvents = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, testEvents);

		var events = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Single(events);
		Assert.True(EventDataComparer.Equal(testEvents[^1], events[0]));
	}

	[Fact]
	public async Task max_count_is_respected() {
		const int  count    = 20;
		const long maxCount = count / 2;

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			StreamState.NoStream,
			Fixture.CreateTestEvents(count)
		);

		var events = await Fixture.Streams
			.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, maxCount)
			.Take(count)
			.ToArrayAsync();

		Assert.Equal(maxCount, events.Length);
	}

	[Fact]
	public async Task populates_log_position_of_event() {
		if (KurrentTemporaryTestNode.Version.Major < 22)
			return;

		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(1).ToArray();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Single(actual);
		Assert.Equal(writeResult.LogPosition.PreparePosition, writeResult.LogPosition.CommitPosition);
		Assert.Equal(writeResult.LogPosition, actual.First().Position);
	}

	[Fact]
	public async Task with_timeout_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamRevision.None, Fixture.CreateTestEvents());

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Streams
				.ReadStreamAsync(
					Direction.Backwards,
					stream,
					StreamPosition.End,
					1,
					false,
					TimeSpan.Zero
				)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task enumeration_referencing_messages_twice_does_not_throw() {
		var result = Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			"$users",
			StreamPosition.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		_ = result.Messages;
		await result.Messages.ToArrayAsync();
	}

	[Fact]
	public async Task enumeration_enumerating_messages_twice_throws() {
		var result = Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			"$users",
			StreamPosition.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		await result.Messages.ToArrayAsync();

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
				await result.Messages.ToArrayAsync()
		);
	}

	[Fact]
	public async Task stream_not_found() {
		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Backwards,
			Fixture.GetStreamName(),
			StreamPosition.End
		).Messages.SingleAsync();

		Assert.Equal(StreamMessage.NotFound.Instance, result);
	}

	[Fact]
	public async Task stream_found() {
		const int eventCount = 32;

		var events = Fixture.CreateTestEvents(eventCount).ToArray();

		var streamName = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await Fixture.Streams.ReadStreamAsync(
			Direction.Backwards,
			streamName,
			StreamPosition.End
		).Messages.ToArrayAsync();

		Assert.Equal(
			eventCount + (Fixture.EventStoreHasLastStreamPosition ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(eventCount, result.OfType<StreamMessage.Event>().Count());

		if (Fixture.EventStoreHasLastStreamPosition)
			Assert.Equal(new StreamMessage.LastStreamPosition(new(31)), result[^1]);
	}
}
