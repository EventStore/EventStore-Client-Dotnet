using System.Text.Json;

namespace EventStore.Client.Streams.Tests;

[Trait("Category", "LongRunning")]
[Trait("Category", "Delete")]
public class deleted_stream(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	static JsonDocument CustomMetadata { get; }

	static deleted_stream() {
		var customMetadata = new Dictionary<string, object> {
			["key1"] = true,
			["key2"] = 17,
			["key3"] = "some value"
		};

		CustomMetadata = JsonDocument.Parse(JsonSerializer.Serialize(customMetadata));
	}
	
	[Fact]
	public async Task reading_throws() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

		await Assert.ThrowsAsync<StreamNotFoundException>(
			() => Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ToArrayAsync().AsTask()
		);
	}

	public static IEnumerable<object?[]> RecreatingTestCases() {
		yield return new object?[] { StreamState.Any, nameof(StreamState.Any) };
		yield return new object?[] { StreamState.NoStream, nameof(StreamState.NoStream) };
	}

	[Theory]
	[MemberData(nameof(RecreatingTestCases))]
	public async Task recreated_with_any_expected_version(StreamState expectedState, string name) {
		var stream = $"{Fixture.GetStreamName()}_{name}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

		var events = Fixture.CreateTestEvents(3).ToArray();

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, expectedState, events);

		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);

		await Task.Delay(50); //TODO: This is a workaround until github issue #1744 is fixed

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(3, actual.Length);
		Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
		Assert.Equal(
			Enumerable.Range(1, 3).Select(i => new StreamPosition((ulong)i)),
			actual.Select(x => x.EventNumber)
		);

		var metadata = await Fixture.Streams.GetStreamMetadataAsync(stream);
		Assert.Equal(new StreamPosition(1), metadata.Metadata.TruncateBefore);
		Assert.Equal(new StreamPosition(1), metadata.MetastreamRevision);
	}

	[Fact]
	public async Task recreated_with_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

		var events = Fixture.CreateTestEvents(3).ToArray();

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			writeResult.NextExpectedStreamRevision,
			events
		);

		Assert.Equal(new(3), writeResult.NextExpectedStreamRevision);

		await Task.Delay(50); //TODO: This is a workaround until github issue #1744 is fixed

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(3, actual.Length);
		Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
		Assert.Equal(
			Enumerable.Range(1, 3).Select(i => new StreamPosition((ulong)i)),
			actual.Select(x => x.EventNumber)
		);

		var metadata = await Fixture.Streams.GetStreamMetadataAsync(stream);
		Assert.Equal(new StreamPosition(1), metadata.Metadata.TruncateBefore);
		Assert.Equal(new StreamPosition(1), metadata.MetastreamRevision);
	}

	[Fact]
	public async Task recreated_preserves_metadata_except_truncate_before() {
		const int count  = 2;

		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(count)
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		var streamMetadata = new StreamMetadata(
			acl: new(deleteRole: "some-role"),
			maxCount: 100,
			truncateBefore: new StreamPosition(long.MaxValue), // 1 less than End
			customMetadata: CustomMetadata
		);

		writeResult = await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			StreamState.NoStream,
			streamMetadata
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(1), events);

		await Task.Delay(500); //TODO: This is a workaround until github issue #1744 is fixed

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(3, actual.Length);
		Assert.Equal(events.Select(x => x.EventId), actual.Select(x => x.EventId));
		Assert.Equal(
			Enumerable.Range(count, 3).Select(i => new StreamPosition((ulong)i)),
			actual.Select(x => x.EventNumber)
		);

		var expected = new StreamMetadata(
			streamMetadata.MaxCount,
			streamMetadata.MaxAge,
			new StreamPosition(2),
			streamMetadata.CacheControl,
			streamMetadata.Acl,
			streamMetadata.CustomMetadata
		);

		var metadataResult = await Fixture.Streams.GetStreamMetadataAsync(stream);
		Assert.Equal(new StreamPosition(1), metadataResult.MetastreamRevision);
		Assert.Equal(expected, metadataResult.Metadata);
	}

	[Fact]
	public async Task can_be_hard_deleted() {
		var stream = Fixture.GetStreamName();

		var writeResult =
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				Fixture.CreateTestEvents(2)
			);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, new StreamRevision(1));

		await Fixture.Streams.TombstoneAsync(stream, StreamState.Any);

		var ex = await Assert.ThrowsAsync<StreamDeletedException>(
			() => Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ToArrayAsync().AsTask()
		);

		Assert.Equal(stream, ex.Stream);

		ex = await Assert.ThrowsAsync<StreamDeletedException>(
			()
				=> Fixture.Streams.GetStreamMetadataAsync(stream)
		);

		Assert.Equal(SystemStreams.MetastreamOf(stream), ex.Stream);

		await Assert.ThrowsAsync<StreamDeletedException>(
			() => Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents())
		);
	}

	[Fact]
	public async Task allows_recreating_for_first_write_only_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(2)
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, new StreamRevision(1));

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(3)
		);

		Assert.Equal(new(4), writeResult.NextExpectedStreamRevision);

		await Assert.ThrowsAsync<WrongExpectedVersionException>(
			() => Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				Fixture.CreateTestEvents()
			)
		);
	}

	[Fact]
	public async Task allows_recreating_for_first_write_only_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, new StreamRevision(1));

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(3)
		);

		Assert.Equal(new(4), writeResult.NextExpectedStreamRevision);

		var wrongExpectedVersionResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(),
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(wrongExpectedVersionResult);
	}

	[Fact]
	public async Task appends_multiple_writes_expected_version_any() {
		var stream = Fixture.GetStreamName();

		var writeResult =
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				Fixture.CreateTestEvents(2)
			);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, new StreamRevision(1));

		var firstEvents  = Fixture.CreateTestEvents(3).ToArray();
		var secondEvents = Fixture.CreateTestEvents(2).ToArray();

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, firstEvents);

		Assert.Equal(new(4), writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, secondEvents);

		Assert.Equal(new(6), writeResult.NextExpectedStreamRevision);

		var actual = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.Select(x => x.Event)
			.ToArrayAsync();

		Assert.Equal(firstEvents.Concat(secondEvents).Select(x => x.EventId), actual.Select(x => x.EventId));
		Assert.Equal(
			Enumerable.Range(2, 5).Select(i => new StreamPosition((ulong)i)),
			actual.Select(x => x.EventNumber)
		);

		var metadataResult = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(new StreamPosition(2), metadataResult.Metadata.TruncateBefore);
		Assert.Equal(new StreamPosition(1), metadataResult.MetastreamRevision);
	}

	[Fact]
	public async Task recreated_on_empty_when_metadata_set() {
		var stream = Fixture.GetStreamName();

		var streamMetadata = new StreamMetadata(
			acl: new(deleteRole: "some-role"),
			maxCount: 100,
			truncateBefore: new StreamPosition(0),
			customMetadata: CustomMetadata
		);

		var writeResult = await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			StreamState.NoStream,
			streamMetadata
		);

		if (GlobalEnvironment.UseCluster)
			// without this delay this test fails sometimes when run against a cluster because
			// when setting metadata on the deleted stream, it creates two new metadata
			// records but not transactionally
			await Task.Delay(200);

		Assert.Equal(new(0), writeResult.NextExpectedStreamRevision);

		await Assert.ThrowsAsync<StreamNotFoundException>(
			() => Fixture.Streams
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ToArrayAsync().AsTask()
		);

		var expected = new StreamMetadata(
			streamMetadata.MaxCount,
			streamMetadata.MaxAge,
			StreamPosition.Start,
			streamMetadata.CacheControl,
			streamMetadata.Acl,
			streamMetadata.CustomMetadata
		);

		var metadataResult = await Fixture.Streams.GetStreamMetadataAsync(stream);
		Assert.Equal(new StreamPosition(0), metadataResult.MetastreamRevision);
		Assert.Equal(expected, metadataResult.Metadata);
	}

	[Fact]
	public async Task recreated_on_non_empty_when_metadata_set() {
		const int count  = 2;

		var stream = Fixture.GetStreamName();

		var streamMetadata = new StreamMetadata(
			acl: new(deleteRole: "some-role"),
			maxCount: 100,
			customMetadata: CustomMetadata
		);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(count)
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		await Fixture.Streams.DeleteAsync(stream, writeResult.NextExpectedStreamRevision);

		writeResult = await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			new StreamRevision(0),
			streamMetadata
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamRevision);

		if (GlobalEnvironment.UseCluster)
			// without this delay this test fails sometimes when run against a cluster because
			// when setting metadata on the deleted stream, it creates two new metadata
			// records, the first one setting the metadata as requested, and the second
			// one adding in the tb. in the window between the two the previously
			// truncated events can be read
			await Task.Delay(200);

		var actual = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToArrayAsync();

		Assert.Empty(actual);

		var metadataResult = await Fixture.Streams.GetStreamMetadataAsync(stream);
		var expected = new StreamMetadata(
			streamMetadata.MaxCount,
			streamMetadata.MaxAge,
			new StreamPosition(count),
			streamMetadata.CacheControl,
			streamMetadata.Acl,
			streamMetadata.CustomMetadata
		);

		Assert.Equal(expected, metadataResult.Metadata);
	}
}