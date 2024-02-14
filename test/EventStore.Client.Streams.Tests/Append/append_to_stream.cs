using System.Text;
using Grpc.Core;

namespace EventStore.Client.Streams.Tests.Append;

[Trait("Category", "Target:Stream")]
[Trait("Category", "Operation:Append")]
public class append_to_stream(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	public static IEnumerable<object?[]> ExpectedVersionCreateStreamTestCases() {
		yield return new object?[] { StreamState.Any };
		yield return new object?[] { StreamState.NoStream };
	}

	[Theory]
	[MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
	public async Task appending_zero_events(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, expectedStreamState, Enumerable.Empty<EventData>());
			writeResult.NextExpectedStreamRevision.ShouldBe(StreamRevision.None);
		}

		await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory]
	[MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
	public async Task appending_zero_events_again(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, expectedStreamState, Enumerable.Empty<EventData>());
			Assert.Equal(StreamRevision.None, writeResult.NextExpectedStreamRevision);
		}

		await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory]
	[MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
	public async Task create_stream_expected_version_on_first_write_if_does_not_exist(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			expectedStreamState,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var count = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2)
			.CountAsync();

		Assert.Equal(1, count);
	}

	[Fact]
	public async Task multiple_idempotent_writes() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(4).ToArray();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);
		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);
		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task multiple_idempotent_writes_with_same_id_bug_case() {
		var stream = Fixture.GetStreamName();

		var evnt   = Fixture.CreateTestEvents().First();
		var events = new[] { evnt, evnt, evnt, evnt, evnt, evnt };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(5), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_any_then_next_expected_version_is_unreliable() {
		var stream = Fixture.GetStreamName();

		var evnt   = Fixture.CreateTestEvents().First();
		var events = new[] { evnt, evnt, evnt, evnt, evnt, evnt };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(5), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_nostream_then_next_expected_version_is_correct() {
		var stream = Fixture.GetStreamName();

		var evnt           = Fixture.CreateTestEvents().First();
		var events         = new[] { evnt, evnt, evnt, evnt, evnt, evnt };
		var streamRevision = StreamRevision.FromInt64(events.Length - 1);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task writing_with_correct_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(1))
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[Fact]
	public async Task returns_log_position_when_writing() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		Assert.True(0 < result.LogPosition.PreparePosition);
		Assert.True(0 < result.LogPosition.CommitPosition);
	}

	[Fact]
	public async Task writing_with_any_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1))
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[Fact]
	public async Task writing_with_invalid_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, new StreamRevision(5), Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[Fact]
	public async Task append_with_correct_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			writeResult.NextExpectedStreamRevision,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task append_with_any_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, new StreamRevision(999), Fixture.CreateTestEvents())
			.ShouldThrowAsync<WrongExpectedVersionException>();

		ex.ActualStreamRevision.ShouldBe(new(0));
		ex.ExpectedStreamRevision.ShouldBe(new(999));
	}

	[Fact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			new StreamRevision(1),
			Fixture.CreateTestEvents(),
			options => { options.ThrowOnAppendFailure = false; }
		);

		var wrongExpectedVersionResult = (WrongExpectedVersionResult)writeResult;

		Assert.Equal(new(1), wrongExpectedVersionResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task append_with_stream_exists_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[Fact]
	public async Task append_with_stream_exists_expected_version_to_stream_with_multiple_events() {
		var stream = Fixture.GetStreamName();

		for (var i = 0; i < 5; i++)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1));

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[Fact]
	public async Task append_with_stream_exists_expected_version_if_metadata_stream_exists() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			StreamState.Any,
			new(10, default)
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[Fact]
	public async Task appending_with_stream_exists_expected_version_and_stream_does_not_exist_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<WrongExpectedVersionException>();
		
		ex.ActualStreamRevision.ShouldBe(StreamRevision.None);
	}

	[Fact]
	public async Task appending_with_stream_exists_expected_version_and_stream_does_not_exist_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents(),
			options => { options.ThrowOnAppendFailure = false; }
		);

		var wrongExpectedVersionResult = Assert.IsType<WrongExpectedVersionResult>(writeResult);

		Assert.Equal(StreamRevision.None, wrongExpectedVersionResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task appending_with_stream_exists_expected_version_to_hard_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[Fact]
	public async Task appending_with_stream_exists_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.DeleteAsync(stream, StreamState.Any);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[Fact]
	public async Task can_append_multiple_events_at_once() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(100));

		Assert.Equal(new(99), writeResult.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task returns_failure_status_when_conditionally_appending_with_version_mismatch() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			new StreamRevision(7),
			Fixture.CreateTestEvents()
		);

		Assert.Equal(
			ConditionalWriteResult.FromWrongExpectedVersion(new(stream, new StreamRevision(7), StreamRevision.None)),
			result
		);
	}

	[Fact]
	public async Task returns_success_status_when_conditionally_appending_with_matching_version() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(
			ConditionalWriteResult.FromWriteResult(new SuccessResult(0, result.LogPosition)),
			result
		);
	}

	[Fact]
	public async Task returns_failure_status_when_conditionally_appending_to_a_deleted_stream() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.TombstoneAsync(stream, StreamState.Any);

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(ConditionalWriteResult.StreamDeleted, result);
	}
	
	[Fact]
	public async Task expected_version_no_stream() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), result!.NextExpectedStreamRevision);
	}

	[Fact]
	public async Task expected_version_no_stream_returns_position() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.True(result.LogPosition > Position.Start);
	}
	
	[Fact]
	public async Task with_timeout_any_stream_revision_fails_when_operation_expired() {
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
	public async Task should_not_append_to_stream_when_error_thrown_midway() {
		var       streamName            = Fixture.GetStreamName();
		const int initialNumberOfEvents = 5;

		// Append some events before
		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			StreamState.Any,
			Fixture.CreateTestEvents(initialNumberOfEvents)
		);

		// Force regular append by passing credentials
		await Assert.ThrowsAsync<EnumerationFailedException>(
			async () =>{
				await Fixture.Streams.AppendToStreamAsync(
					streamName,
					StreamState.StreamExists,
					GetEvents(),
					userCredentials: new UserCredentials("admin", "changeit")
				);
			}
		);

		// No more events should be appended to the stream
		var eventsCount = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start)
			.CountAsync();
		eventsCount.ShouldBe(initialNumberOfEvents, "No more events should be appended to the stream");

		return;

		// Throw an exception after 5 events
		IEnumerable<EventData> GetEvents() {
			for (var i = 0; i < 5; i++) {
				yield return Fixture.CreateTestEvents(1).First();
			}

			throw new EnumerationFailedException();
		}
	}

	[Fact]
	public async Task with_timeout_stream_revision_fails_when_operation_expired() {
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
	
	[Fact]
	public async Task when_events_enumerator_throws_the_write_does_not_succeed() {
		var streamName = Fixture.GetStreamName();

		await Fixture.Streams
			.AppendToStreamAsync(streamName, StreamRevision.None, GetEvents())
			.ShouldThrowAsync<EnumerationFailedException>();

		var result = Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);

		var state = await result.ReadState;

		state.ShouldBe(ReadState.StreamNotFound);
		
		return;

		IEnumerable<EventData> GetEvents() {
			var i = 0;
			foreach (var evt in Fixture.CreateTestEvents(5)) {
				if (i++ % 3 == 0)
					throw new EnumerationFailedException();

				yield return evt;
			}
		}
	}

	class EnumerationFailedException : Exception { }
	
	public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
		yield return new object?[] { StreamState.Any };
		yield return new object?[] { ulong.MaxValue - 1UL };
	}
}
