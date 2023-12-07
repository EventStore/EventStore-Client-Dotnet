using System.Text;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "LongRunning")]
[Trait("Category", "AllStream")]
[Trait("Category", "Read")]
[Trait("Category", "ReadForwards")]
public class read_all_events_forward(ITestOutputHelper output, ReadAllEventsFixture fixture) : EventStoreTests<ReadAllEventsFixture>(output, fixture) {
	[Fact]
	public async Task return_empty_if_reading_from_end() {
		var result = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.End, 1).CountAsync();
		result.ShouldBe(0);
	}

	[Fact]
	public async Task return_partial_slice_if_not_enough_events() {
		var sliceSize = Fixture.ExpectedEvents.Length * 2;
		var result    = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, sliceSize).ToArrayAsync();
		result.Length.ShouldBeLessThan(sliceSize);
	}

	[Fact]
	public async Task return_events_in_correct_order_compared_to_written() {
		// TODO SS: this test must be fixed to deterministically read the expected events regardless of how many already exist. reading all for now...
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start)
			.Where(x => x.OriginalStreamId == Fixture.ExpectedStreamName)
			.ToBinaryData();
		
		result.ShouldBe(Fixture.ExpectedEvents);
	}

	[Fact]
	public async Task return_single_event() {
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.ToArrayAsync();
		
		result.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task max_count_is_respected() {
		var maxCount = Fixture.ExpectedEvents.Length / 2;
		var result = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, maxCount)
			.Take(Fixture.ExpectedEvents.Length)
			.ToArrayAsync();

		result.Length.ShouldBe(maxCount);
	}

	[Fact]
	public async Task reads_all_events_by_default() {
		var count = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start)
			.CountAsync();

		Assert.True(count >= Fixture.ExpectedEvents.Length);
	}
	
	[Fact]
	public async Task stream_found() {
		var count = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start)
			.Where(x => x.OriginalStreamId == Fixture.ExpectedStreamName)
			.CountAsync();
	
		count.ShouldBe(Fixture.ExpectedEvents.Length);
	}
	
	[Fact]
	public async Task with_linkto_passed_max_count_one_event_is_read() {
		const string deletedStream = nameof(deletedStream);
		const string linkedStream  = nameof(linkedStream);
	
		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.SetStreamMetadataAsync(
			deletedStream,
			StreamState.Any,
			new(2)
		);

		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Streams.AppendToStreamAsync(
			linkedStream,
			StreamState.Any,
			new[] {
				new EventData(
					Uuid.NewUuid(),
					SystemEventTypes.LinkTo,
					Encoding.UTF8.GetBytes($"0@{deletedStream}"),
					Array.Empty<byte>(),
					Constants.Metadata.ContentTypes.ApplicationOctetStream
				)
			}
		);

		var events = await Fixture.Streams.ReadStreamAsync(
				Direction.Forwards,
				linkedStream,
				StreamPosition.Start,
				resolveLinkTos: true
			)
			.ToArrayAsync();
		
		Assert.Single(events);
	}
	
	[Fact]
	public async Task enumeration_all_referencing_messages_twice_does_not_throw() {
		var result = Fixture.Streams.ReadAllAsync(
			Direction.Forwards,
			Position.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		_ = result.Messages;
		await result.Messages.ToArrayAsync();
	}

	[Fact]
	public async Task enumeration_all_enumerating_messages_twice_throws() {
		var result = Fixture.Streams.ReadAllAsync(
			Direction.Forwards,
			Position.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		await result.Messages.ToArrayAsync();

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
				await result.Messages.ToArrayAsync()
		);
	}
	
	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_all_one_by_one_until_end_of_stream() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_events_slice_at_time() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task when_got_int_max_value_as_maxcount_should_throw() => throw new NotImplementedException();
}