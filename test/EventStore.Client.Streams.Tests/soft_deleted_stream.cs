using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class soft_deleted_stream : IClassFixture<soft_deleted_stream.Fixture> {
		private readonly Fixture _fixture;
		private readonly JsonDocument _customMetadata;

		public soft_deleted_stream(Fixture fixture) {
			_fixture = fixture;

			var customMetadata = new Dictionary<string, object> {
				["key1"] = true,
				["key2"] = 17,
				["key3"] = "some value"
			};

			_customMetadata = JsonDocument.Parse(JsonSerializer.Serialize(customMetadata));
		}

		[Fact]
		public async Task reading_throws() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, writeResult.NextExpectedStreamRevision);

			await Assert.ThrowsAsync<StreamNotFoundException>(
				() => _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
					.ToArrayAsync().AsTask());
		}

		public static IEnumerable<object[]> RecreatingTestCases() {
			yield return new object[] {StreamState.Any, nameof(StreamState.Any)};
			yield return new object[] {StreamState.NoStream, nameof(StreamState.NoStream)};
		}

		[Theory, MemberData(nameof(RecreatingTestCases))]
		public async Task recreated_with_any_expected_version(
			StreamState expectedState, string name) {
			var stream = $"{_fixture.GetStreamName()}_{name}";

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, writeResult.NextExpectedStreamRevision);

			var events = _fixture.CreateTestEvents(3).ToArray();

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, expectedState, events);

			Assert.Equal(new StreamRevision(3), writeResult.NextExpectedStreamRevision);

			await Task.Delay(50); //TODO: This is a workaround until github issue #1744 is fixed

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.Equal(3, actual.Length);
			Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
			Assert.Equal(
				Enumerable.Range(1, 3).Select(i => new StreamPosition((ulong)i)),
				actual.Select(x => x.EventNumber));

			var metadata = await _fixture.Client.GetStreamMetadataAsync(stream);
			Assert.Equal(new StreamPosition(1), metadata.Metadata.TruncateBefore);
			Assert.Equal(new StreamPosition(1), metadata.MetastreamRevision);
		}

		[Fact]
		public async Task recreated_with_expected_version() {
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents());

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, writeResult.NextExpectedStreamRevision);

			var events = _fixture.CreateTestEvents(3).ToArray();

			writeResult = await _fixture.Client.AppendToStreamAsync(stream,
				writeResult.NextExpectedStreamRevision, events);

			Assert.Equal(new StreamRevision(3), writeResult.NextExpectedStreamRevision);

			await Task.Delay(50); //TODO: This is a workaround until github issue #1744 is fixed

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.Equal(3, actual.Length);
			Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
			Assert.Equal(
				Enumerable.Range(1, 3).Select(i => new StreamPosition((ulong)i)),
				actual.Select(x => x.EventNumber));

			var metadata = await _fixture.Client.GetStreamMetadataAsync(stream);
			Assert.Equal(new StreamPosition(1), metadata.Metadata.TruncateBefore);
			Assert.Equal(new StreamPosition(1), metadata.MetastreamRevision);
		}

		[Fact]
		public async Task recreated_preserves_metadata_except_truncate_before() {
			const int count = 2;
			var stream = _fixture.GetStreamName();

			var writeResult = await _fixture.Client.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			var streamMetadata = new StreamMetadata(
				acl: new StreamAcl(deleteRole: "some-role"),
				maxCount: 100,
				truncateBefore: new StreamPosition(long.MaxValue), // 1 less than End
				customMetadata: _customMetadata);
			writeResult = await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.NoStream,
				streamMetadata);
			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			var events = _fixture.CreateTestEvents(3).ToArray();

			await _fixture.Client.AppendToStreamAsync(stream, new StreamRevision(1), events);

			await Task.Delay(500); //TODO: This is a workaround until github issue #1744 is fixed

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.Equal(3, actual.Length);
			Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
			Assert.Equal(
				Enumerable.Range(count, 3).Select(i => new StreamPosition((ulong)i)),
				actual.Select(x => x.EventNumber));

			var expected = new StreamMetadata(streamMetadata.MaxCount, streamMetadata.MaxAge, new StreamPosition(2),
				streamMetadata.CacheControl, streamMetadata.Acl, streamMetadata.CustomMetadata);
			var metadataResult = await _fixture.Client.GetStreamMetadataAsync(stream);
			Assert.Equal(new StreamPosition(1), metadataResult.MetastreamRevision);
			Assert.Equal(expected, metadataResult.Metadata);
		}

		[Fact]
		public async Task can_be_hard_deleted() {
			var stream = _fixture.GetStreamName();

			var writeResult =
				await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
					_fixture.CreateTestEvents(2));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, new StreamRevision(1));

			await _fixture.Client.TombstoneAsync(stream, StreamState.Any);

			var ex = await Assert.ThrowsAsync<StreamDeletedException>(
				() => _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
					.ToArrayAsync().AsTask());

			Assert.Equal(stream, ex.Stream);

			ex = await Assert.ThrowsAsync<StreamDeletedException>(()
				=> _fixture.Client.GetStreamMetadataAsync(stream));

			Assert.Equal(SystemStreams.MetastreamOf(stream), ex.Stream);

			await Assert.ThrowsAsync<StreamDeletedException>(
				() => _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, _fixture.CreateTestEvents()));
		}
		
		[Fact]
		public async Task allows_recreating_for_first_write_only_throws_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			var writeResult =
				await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
					_fixture.CreateTestEvents(2));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, new StreamRevision(1));

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents(3));

			Assert.Equal(new StreamRevision(4), writeResult.NextExpectedStreamRevision);

			await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
					_fixture.CreateTestEvents()));
		}

		[Fact]
		public async Task allows_recreating_for_first_write_only_returns_wrong_expected_version() {
			var stream = _fixture.GetStreamName();

			var writeResult =
				await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents(2));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, new StreamRevision(1));

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents(3));

			Assert.Equal(new StreamRevision(4), writeResult.NextExpectedStreamRevision);

			var wrongExpectedVersionResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
					_fixture.CreateTestEvents(), options => options.ThrowOnAppendFailure = false);
			
			Assert.IsType<WrongExpectedVersionResult>(wrongExpectedVersionResult);
		}

		[Fact]
		public async Task appends_multiple_writes_expected_version_any() {
			var stream = _fixture.GetStreamName();

			var writeResult =
				await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
					_fixture.CreateTestEvents(2));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, new StreamRevision(1));

			var firstEvents = _fixture.CreateTestEvents(3).ToArray();
			var secondEvents = _fixture.CreateTestEvents(2).ToArray();

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, firstEvents);

			Assert.Equal(new StreamRevision(4), writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.Any, secondEvents);

			Assert.Equal(new StreamRevision(6), writeResult.NextExpectedStreamRevision);

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.Equal(firstEvents.Concat(secondEvents).Select(x => x.EventId), actual.Select(x => x.EventId));
			Assert.Equal(Enumerable.Range(2, 5).Select(i => new StreamPosition((ulong)i)),
				actual.Select(x => x.EventNumber));

			var metadataResult = await _fixture.Client.GetStreamMetadataAsync(stream);

			Assert.Equal(new StreamPosition(2), metadataResult.Metadata.TruncateBefore);
			Assert.Equal(new StreamPosition(1), metadataResult.MetastreamRevision);
		}

		[Fact]
		public async Task recreated_on_empty_when_metadata_set() {
			var stream = _fixture.GetStreamName();

			var streamMetadata = new StreamMetadata(
				acl: new StreamAcl(deleteRole: "some-role"),
				maxCount: 100,
				truncateBefore: new StreamPosition(0),
				customMetadata: _customMetadata);

			var writeResult = await _fixture.Client.SetStreamMetadataAsync(
				stream,
				StreamState.NoStream,
				streamMetadata);

			if (GlobalEnvironment.UseCluster) {
				// without this delay this test fails sometimes when run against a cluster because
				// when setting metadata on the deleted stream, it creates two new metadata
				// records but not transactionally
				await Task.Delay(200);
			}

			Assert.Equal(new StreamRevision(0), writeResult.NextExpectedStreamRevision);

			await Assert.ThrowsAsync<StreamNotFoundException>(() => _fixture.Client
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ToArrayAsync().AsTask());

			var expected = new StreamMetadata(streamMetadata.MaxCount, streamMetadata.MaxAge, StreamPosition.Start,
				streamMetadata.CacheControl, streamMetadata.Acl, streamMetadata.CustomMetadata);

			var metadataResult = await _fixture.Client.GetStreamMetadataAsync(stream);
			Assert.Equal(new StreamPosition(0), metadataResult.MetastreamRevision);
			Assert.Equal(expected, metadataResult.Metadata);
		}


		[Fact]
		public async Task recreated_on_non_empty_when_metadata_set() {
			const int count = 2;
			var stream = _fixture.GetStreamName();

			var streamMetadata = new StreamMetadata(
				acl: new StreamAcl(deleteRole: "some-role"),
				maxCount: 100,
				customMetadata: _customMetadata);

			var writeResult = await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			await _fixture.Client.SoftDeleteAsync(stream, writeResult.NextExpectedStreamRevision);

			writeResult = await _fixture.Client.SetStreamMetadataAsync(
				stream,
				new StreamRevision(0),
				streamMetadata);

			Assert.Equal(new StreamRevision(1), writeResult.NextExpectedStreamRevision);

			if (GlobalEnvironment.UseCluster) {
				// without this delay this test fails sometimes when run against a cluster because
				// when setting metadata on the deleted stream, it creates two new metadata
				// records, the first one setting the metadata as requested, and the second
				// one adding in the tb. in the window between the two the previously
				// truncated events can be read
				await Task.Delay(200);
			}

			var actual = await _fixture.Client
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ToArrayAsync();

			Assert.Empty(actual);

			var metadataResult = await _fixture.Client.GetStreamMetadataAsync(stream);
			var expected = new StreamMetadata(streamMetadata.MaxCount, streamMetadata.MaxAge, new StreamPosition(count),
				streamMetadata.CacheControl, streamMetadata.Acl, streamMetadata.CustomMetadata);
			Assert.Equal(expected, metadataResult.Metadata);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
