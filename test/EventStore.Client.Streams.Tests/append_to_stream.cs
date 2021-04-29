using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class append_to_stream : IClassFixture<append_to_stream.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream(Fixture fixture) {
			_fixture = fixture;
		}

		public static IEnumerable<object[]> ExpectedVersionCreateStreamTestCases() {
			yield return new object[] {StreamState.Any};
			yield return new object[] {StreamState.NoStream};
		}

		[Theory, MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
		public async Task appending_zero_events(StreamState expectedStreamState) {
			var stream = $"{_fixture.GetStreamName()}_{expectedStreamState}";

			const int iterations = 2;
			for (var i = 0; i < iterations; i++) {
				var writeResult = await _fixture.Client.AppendToStreamAsync(
					stream, expectedStreamState, Enumerable.Empty<EventData>());
				Assert.Equal(StreamRevision.None, writeResult.NextExpectedStreamRevision);
			}

			var ex = await Assert.ThrowsAsync<StreamNotFoundException>(() =>
				_fixture.Client
					.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
					.ToArrayAsync().AsTask());

			Assert.Equal(stream, ex.Stream);
		}

		[Theory, MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
		public async Task appending_zero_events_again(StreamState expectedStreamState) {
			var stream = $"{_fixture.GetStreamName()}_{expectedStreamState}";

			const int iterations = 2;
			for (var i = 0; i < iterations; i++) {
				var writeResult = await _fixture.Client.AppendToStreamAsync(
					stream, expectedStreamState, Enumerable.Empty<EventData>());
				Assert.Equal(StreamRevision.None, writeResult.NextExpectedStreamRevision);
			}

			var ex = await Assert.ThrowsAsync<StreamNotFoundException>(() =>
				_fixture.Client
					.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
					.ToArrayAsync().AsTask());

			Assert.Equal(stream, ex.Stream);
		}

		[Theory, MemberData(nameof(ExpectedVersionCreateStreamTestCases))]
		public async Task create_stream_expected_version_on_first_write_if_does_not_exist(
			StreamState expectedStreamState) {
			var stream = $"{_fixture.GetStreamName()}_{expectedStreamState}";

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				expectedStreamState,
				_fixture.CreateTestEvents(1));

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			var count = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2)
				.CountAsync();
			Assert.Equal(1, count);
		}

		[Fact]
		public async Task multiple_idempotent_writes() {
			var stream = _fixture.GetStreamName();
			var events = _fixture.CreateTestEvents(4).ToArray();

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, events);
			Assert.Equal(new StreamRevision(3), writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, events);
			Assert.Equal(new StreamRevision(3), writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task multiple_idempotent_writes_with_same_id_bug_case() {
			var stream = _fixture.GetStreamName();

			var evnt = _fixture.CreateTestEvents().First();
			var events = new[] {evnt, evnt, evnt, evnt, evnt, evnt};

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, events);

			Assert.Equal(new StreamRevision(5), writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task
			in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_any_then_next_expected_version_is_unreliable() {
			var stream = _fixture.GetStreamName();

			var evnt = _fixture.CreateTestEvents().First();
			var events = new[] {evnt, evnt, evnt, evnt, evnt, evnt};

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, events);

			Assert.Equal(new StreamRevision(5), writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, events);

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task
			in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_nostream_then_next_expected_version_is_correct() {
			var stream = _fixture.GetStreamName();

			var evnt = _fixture.CreateTestEvents().First();
			var events = new[] {evnt, evnt, evnt, evnt, evnt, evnt};
			var streamRevision = StreamRevision.FromInt64(events.Length - 1);

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, events);

			Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, events);

			Assert.Equal(streamRevision, writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task writing_with_correct_expected_version_to_deleted_stream_throws_stream_deleted() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(() => _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents(1)));
		}

		[Fact]
		public async Task returns_log_position_when_writing() {
			var stream = _fixture.GetStreamName();

			var result = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents(1));
			Assert.True(0 < result.LogPosition.PreparePosition);
			Assert.True(0 < result.LogPosition.CommitPosition);
		}

		[Fact]
		public async Task writing_with_any_expected_version_to_deleted_stream_throws_stream_deleted() {
			var stream = _fixture.GetStreamName();
			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(
				() => _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents(1)));
		}

		[Fact]
		public async Task writing_with_invalid_expected_version_to_deleted_stream_throws_stream_deleted() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(
				() => _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(5), _fixture.CreateTestEvents()));
		}

		[Fact]
		public async Task append_with_correct_expected_version_to_existing_stream() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents(1));

			writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				writeResult.NextExpectedStreamRevision,
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task append_with_any_expected_version_to_existing_stream() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents(1));

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				_fixture.CreateTestEvents(1));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);
		}
		
		[Fact]
		public async Task appending_with_wrong_expected_version_to_existing_stream_throws_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(999), _fixture.CreateTestEvents()));
			Assert.Equal(new StreamRevision(0), ex.ActualStreamRevision);
			Assert.Equal(new StreamRevision(999), ex.ExpectedStreamRevision);
		}

		[Fact]
		public async Task appending_with_wrong_expected_version_to_existing_stream_returns_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(1),
				_fixture.CreateTestEvents(), options => {
					options.ThrowOnAppendFailure = false;
				});

			var wrongExpectedVersionResult = (WrongExpectedVersionResult)writeResult;
			
			Assert.Equal(new StreamRevision(1), wrongExpectedVersionResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task append_with_stream_exists_expected_version_to_existing_stream() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.StreamExists,
				_fixture.CreateTestEvents());
		}

		[Fact]
		public async Task append_with_stream_exists_expected_version_to_stream_with_multiple_events() {
			var stream = _fixture.GetStreamName();

			for (var i = 0; i < 5; i++) {
				await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents(1));
			}

			await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.StreamExists,
				_fixture.CreateTestEvents());
		}

		[Fact]
		public async Task append_with_stream_exists_expected_version_if_metadata_stream_exists() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.Any,
				new StreamMetadata(10, default));

			await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.StreamExists,
				_fixture.CreateTestEvents());
		}
		
		[Fact]
		public async Task
			appending_with_stream_exists_expected_version_and_stream_does_not_exist_throws_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => _fixture.Client.AppendToStreamAsync(stream, StreamState.StreamExists,
					_fixture.CreateTestEvents()));

			Assert.Equal(StreamRevision.None, ex.ActualStreamRevision);
		}

		[Fact]
		public async Task
			appending_with_stream_exists_expected_version_and_stream_does_not_exist_returns_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.StreamExists,
				_fixture.CreateTestEvents(), options => {
					options.ThrowOnAppendFailure = false;
				});
			
			var wrongExpectedVersionResult = Assert.IsType<WrongExpectedVersionResult>(writeResult);

			Assert.Equal(StreamRevision.None, wrongExpectedVersionResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task appending_with_stream_exists_expected_version_to_hard_deleted_stream_throws_stream_deleted() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(() => _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.StreamExists,
				_fixture.CreateTestEvents()));
		}

		[Fact]
		public async Task appending_with_stream_exists_expected_version_to_soft_deleted_stream_throws_stream_deleted() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.SoftDeleteAsync(stream, StreamState.NoStream);

			await Assert.ThrowsAsync<StreamDeletedException>(() => _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.StreamExists,
				_fixture.CreateTestEvents()));
		}

		[Fact]
		public async Task can_append_multiple_events_at_once() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream, StreamState.NoStream, _fixture.CreateTestEvents(100));

			Assert.Equal(new StreamRevision(99), writeResult.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task returns_failure_status_when_conditionally_appending_with_version_mismatch() {
			var stream = _fixture.GetStreamName();

			var result = await _fixture.Client.ConditionalAppendToStreamAsync(stream, new StreamRevision(7),
				_fixture.CreateTestEvents());

			Assert.Equal(ConditionalWriteResult.FromWrongExpectedVersion(
					new WrongExpectedVersionException(stream, new StreamRevision(7), StreamRevision.None)),
				result);
		}

		[Fact]
		public async Task returns_success_status_when_conditionally_appending_with_matching_version() {
			var stream = _fixture.GetStreamName();

			var result = await _fixture.Client.ConditionalAppendToStreamAsync(stream, StreamState.Any,
				_fixture.CreateTestEvents());

			Assert.Equal(ConditionalWriteResult.FromWriteResult(new SuccessResult(0, result.LogPosition)),
				result);
		}

		[Fact]
		public async Task returns_failure_status_when_conditionally_appending_to_a_deleted_stream() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			await _fixture.Client.TombstoneAsync(stream, StreamState.Any);

			var result = await _fixture.Client.ConditionalAppendToStreamAsync(stream, StreamState.Any,
				_fixture.CreateTestEvents());

			Assert.Equal(ConditionalWriteResult.StreamDeleted, result);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
