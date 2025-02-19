using System.Text.Json;
using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Metadata")]
public class StreamMetadataTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[Fact]
	public async Task getting_for_an_existing_stream_and_no_metadata_exists() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		var actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(StreamMetadataResult.None(stream), actual);
	}

	[Fact]
	public async Task empty_metadata() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, new());

		var actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(stream, actual.StreamName);
		Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
		Assert.False(actual.StreamDeleted);
		Assert.Equal(
			"{}",
			JsonSerializer.Serialize(
				actual.Metadata,
				new JsonSerializerOptions {
					Converters = { StreamMetadataJsonConverter.Instance }
				}
			)
		);
	}

	[Fact]
	public async Task latest_metadata_is_returned() {
		var stream = Fixture.GetStreamName();

		var expected = new StreamMetadata(
			17,
			TimeSpan.FromSeconds(0xDEADBEEF),
			new StreamPosition(10),
			TimeSpan.FromSeconds(0xABACABA)
		);

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.NoStream, expected);

		var actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(stream, actual.StreamName);
		Assert.False(actual.StreamDeleted);
		Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
		Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
		Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
		Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
		Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
		Assert.Equal(expected.Acl, actual.Metadata.Acl);

		expected = new(
			37,
			TimeSpan.FromSeconds(0xBEEFDEAD),
			new StreamPosition(24),
			TimeSpan.FromSeconds(0xDABACABAD)
		);

		await Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(0), expected);

		actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(stream, actual.StreamName);
		Assert.False(actual.StreamDeleted);
		Assert.Equal(new StreamPosition(1), actual.MetastreamRevision);
		Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
		Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
		Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
		Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
		Assert.Equal(expected.Acl, actual.Metadata.Acl);
	}

	[Fact]
	public async Task setting_with_wrong_expected_version_throws() {
		var stream = Fixture.GetStreamName();
		await Assert.ThrowsAsync<WrongExpectedVersionException>(
			() =>
				Fixture.Streams.SetStreamMetadataAsync(stream, new StreamRevision(2), new())
		);
	}

	[Fact]
	public async Task setting_with_wrong_expected_version_returns() {
		var stream = Fixture.GetStreamName();
		var writeResult =
			await Fixture.Streams.SetStreamMetadataAsync(
				stream,
				new StreamRevision(2),
				new(),
				options => { options.ThrowOnAppendFailure = false; }
			);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	[Fact]
	public async Task latest_metadata_returned_stream_revision_any() {
		var stream = Fixture.GetStreamName();

		var expected = new StreamMetadata(
			17,
			TimeSpan.FromSeconds(0xDEADBEEF),
			new StreamPosition(10),
			TimeSpan.FromSeconds(0xABACABA)
		);

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.Any, expected);

		var actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(stream, actual.StreamName);
		Assert.False(actual.StreamDeleted);
		Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
		Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
		Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
		Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
		Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
		Assert.Equal(expected.Acl, actual.Metadata.Acl);

		expected = new(
			37,
			TimeSpan.FromSeconds(0xBEEFDEAD),
			new StreamPosition(24),
			TimeSpan.FromSeconds(0xDABACABAD)
		);

		await Fixture.Streams.SetStreamMetadataAsync(stream, StreamState.Any, expected);

		actual = await Fixture.Streams.GetStreamMetadataAsync(stream);

		Assert.Equal(stream, actual.StreamName);
		Assert.False(actual.StreamDeleted);
		Assert.Equal(new StreamPosition(1), actual.MetastreamRevision);
		Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
		Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
		Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
		Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
		Assert.Equal(expected.Acl, actual.Metadata.Acl);
	}

	[Fact]
	public async Task with_timeout_set_with_any_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.SetStreamMetadataAsync(
					stream,
					StreamState.Any,
					new(),
					deadline: TimeSpan.Zero
				)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task with_timeout_set_with_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var rpcException = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Streams.SetStreamMetadataAsync(
					stream,
					new StreamRevision(0),
					new(),
					deadline: TimeSpan.Zero
				)
		);

		Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
	}

	[Fact]
	public async Task with_timeout_get_fails_when_operation_expired() {
		var stream       = Fixture.GetStreamName();
		var rpcException = await Assert.ThrowsAsync<RpcException>(() => Fixture.Streams.GetStreamMetadataAsync(stream, TimeSpan.Zero));
	}
}
