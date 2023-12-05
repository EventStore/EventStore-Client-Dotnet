namespace EventStore.Client.Streams.Tests;

[LongRunning]
public class read_all_events_forward : IClassFixture<ReadAllEventsForward> {
	public read_all_events_forward(ITestOutputHelper output, ReadAllEventsForward fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	ReadAllEventsForward Fixture { get; }

	[Fact]
	public async Task return_empty_if_reading_from_end() {
		var count = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.End, 1).CountAsync();
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task return_partial_slice_if_not_enough_events() {
		var events = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, Fixture.Events.Length * 2)
			.ToArrayAsync();

		Assert.True(events.Length < Fixture.Events.Length * 2);
	}

	[Fact]
	public async Task return_events_in_correct_order_compared_to_written() {
		var events = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, Fixture.Events.Length * 2)
			.ToArrayAsync();

		Assert.True(EventDataComparer.Equal(Fixture.Events, events.AsResolvedTestEvents().ToArray()));
	}

	[Fact]
	public async Task return_single_event() {
		var events = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, 1)
			.ToArrayAsync();

		Assert.Single(events);
	}

	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_all_one_by_one_until_end_of_stream() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task be_able_to_read_events_slice_at_time() => throw new NotImplementedException();

	[Fact(Skip = "Not Implemented")]
	public Task when_got_int_max_value_as_maxcount_should_throw() => throw new NotImplementedException();

	[Fact]
	public async Task max_count_is_respected() {
		var maxCount = Fixture.Events.Length / 2;
		var events = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start, maxCount)
			.Take(Fixture.Events.Length)
			.ToArrayAsync();

		Assert.Equal(maxCount, events.Length);
	}

	[Fact]
	public async Task reads_all_events_by_default() {
		var count = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start)
			.CountAsync();

		Assert.True(count >= Fixture.Events.Length);
	}
}

public class ReadAllEventsForward : EventStoreFixture {
	public ReadAllEventsForward() {
		OnSetup = async () => {
			Events = Enumerable
				.Concat(
					CreateTestEvents(20),
					CreateTestEvents(2, metadataSize: 1_000_000)
				)
				.ToArray();

			var streamName = GetStreamName();

			var result = await Streams.SetStreamMetadataAsync(
				SystemStreams.AllStream,
				StreamState.NoStream,
				new(acl: new(SystemRoles.All)),
				userCredentials: TestCredentials.Root
			);

			await Streams.AppendToStreamAsync(streamName, StreamState.NoStream, Events);
		};
	}

	public EventData[] Events { get; private set; } = Array.Empty<EventData>();
}