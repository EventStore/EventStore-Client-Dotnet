using EventStore.Client.Tests.TestNode;
using Grpc.Core;

namespace EventStore.Client.Tests;

[Trait("Category", "Target:All")]
[Trait("Category", "Operation:Read")]
[Trait("Category", "Operation:Read:Backwards")]
[Trait("Category", "Database:Dedicated")]
public class ReadAllEventsBackwardTests(ITestOutputHelper output, ReadAllEventsFixture fixture)
	: KurrentTemporaryTests<ReadAllEventsFixture>(output, fixture) {
	[Fact]
	public async Task return_empty_if_reading_from_start() {
		var result = await Fixture.Streams.ReadAllAsync(Direction.Backwards, Position.Start, 1).CountAsync();
		result.ShouldBe(0);
	}

	[Fact]
	public async Task return_partial_slice_if_not_enough_events() {
		var count     = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start).CountAsync();
		var sliceSize = count * 2;
		var result    = await Fixture.Streams.ReadAllAsync(Direction.Backwards, Position.End, sliceSize).ToArrayAsync();
		result.Length.ShouldBeLessThan(sliceSize);
	}

	[Fact]
	public async Task return_events_in_reversed_order_compared_to_written() {
		// TODO SS: this test must be fixed to deterministically read the expected events regardless of how many already exist. reading all for now...
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.End)
			.Where(x => x.OriginalStreamId == Fixture.ExpectedStreamName)
			.ToBinaryData();

		result.ShouldBe(Fixture.ExpectedEventsReversed);
	}

	[Fact]
	public async Task return_single_event() {
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.End, 1)
			.ToArrayAsync();

		result.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task max_count_is_respected() {
		var maxCount = Fixture.ExpectedEvents.Length / 2;
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.End, maxCount)
			.Take(Fixture.ExpectedEvents.Length)
			.ToArrayAsync();

		result.Length.ShouldBe(maxCount);
	}

	[Fact]
	public async Task stream_found() {
		var count = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.End)
			.Where(x => x.OriginalStreamId == Fixture.ExpectedStreamName)
			.CountAsync();

		count.ShouldBe(Fixture.ExpectedEvents.Length);
	}

	[Fact]
	public async Task with_timeout_fails_when_operation_expired() {
		var ex = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.Start, 1, false, TimeSpan.Zero)
			.ToArrayAsync()
			.AsTask().ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	[Fact]
	public async Task filter_events_by_type() {
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Backwards, Position.End, EventTypeFilter.Prefix(KurrentTemporaryFixture.AnotherTestEventTypePrefix))
			.ToListAsync();

		result.ForEach(x => x.Event.EventType.ShouldStartWith(KurrentTemporaryFixture.AnotherTestEventTypePrefix));
	}

	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_all_one_by_one_until_end_of_stream() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_events_slice_at_time() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task when_got_int_max_value_as_maxcount_should_throw() => throw new NotImplementedException();
}
