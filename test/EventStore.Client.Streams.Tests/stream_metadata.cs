using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class stream_metadata : IClassFixture<stream_metadata.Fixture> {
		private readonly Fixture _fixture;

		public stream_metadata(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task getting_for_an_existing_stream_and_no_metadata_exists() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, _fixture.CreateTestEvents());

			var actual = await _fixture.Client.GetStreamMetadataAsync(stream);

			Assert.Equal(StreamMetadataResult.None(stream), actual);
		}

		[Fact]
		public async Task empty_metadata() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.NoStream, new StreamMetadata());

			var actual = await _fixture.Client.GetStreamMetadataAsync(stream);

			Assert.Equal(stream, actual.StreamName);
			Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
			Assert.False(actual.StreamDeleted);
			Assert.Equal("{}", JsonSerializer.Serialize(actual.Metadata,
				new JsonSerializerOptions {
					Converters = {StreamMetadataJsonConverter.Instance}
				}));
		}

		[Fact]
		public async Task latest_metadata_is_returned() {
			var stream = _fixture.GetStreamName();

			var expected = new StreamMetadata(17, TimeSpan.FromSeconds(0xDEADBEEF), new StreamPosition(10),
				TimeSpan.FromSeconds(0xABACABA));

			await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.NoStream, expected);

			var actual = await _fixture.Client.GetStreamMetadataAsync(stream);
			
			Assert.Equal(stream, actual.StreamName);
			Assert.False(actual.StreamDeleted);
			Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
			Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
			Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
			Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
			Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
			Assert.Equal(expected.Acl, actual.Metadata.Acl);
			
			expected = new StreamMetadata(37, TimeSpan.FromSeconds(0xBEEFDEAD), new StreamPosition(24),
				TimeSpan.FromSeconds(0xDABACABAD));

			await _fixture.Client.SetStreamMetadataAsync(stream, new StreamRevision(0), expected);

			actual = await _fixture.Client.GetStreamMetadataAsync(stream);
			
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
			var stream = _fixture.GetStreamName();
			await Assert.ThrowsAsync<WrongExpectedVersionException>(() =>
				_fixture.Client.SetStreamMetadataAsync(stream, new StreamRevision(2), new StreamMetadata()));
		}

		[Fact]
		public async Task setting_with_wrong_expected_version_returns() {
			var stream = _fixture.GetStreamName();
			var writeResult =
				await _fixture.Client.SetStreamMetadataAsync(stream, new StreamRevision(2), new StreamMetadata(),
					options => {
						options.ThrowOnAppendFailure = false;
				});

			Assert.IsType<WrongExpectedVersionResult>(writeResult);
		}

		[Fact]
		public async Task latest_metadata_returned_stream_revision_any() {
			var stream = _fixture.GetStreamName();

			var expected = new StreamMetadata(17, TimeSpan.FromSeconds(0xDEADBEEF), new StreamPosition(10),
				TimeSpan.FromSeconds(0xABACABA));

			await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.Any, expected);

			var actual = await _fixture.Client.GetStreamMetadataAsync(stream);
			
			Assert.Equal(stream, actual.StreamName);
			Assert.False(actual.StreamDeleted);
			Assert.Equal(StreamPosition.Start, actual.MetastreamRevision);
			Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
			Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
			Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
			Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
			Assert.Equal(expected.Acl, actual.Metadata.Acl);
			
			expected = new StreamMetadata(37, TimeSpan.FromSeconds(0xBEEFDEAD), new StreamPosition(24),
				TimeSpan.FromSeconds(0xDABACABAD));

			await _fixture.Client.SetStreamMetadataAsync(stream, StreamState.Any, expected);

			actual = await _fixture.Client.GetStreamMetadataAsync(stream);
			
			Assert.Equal(stream, actual.StreamName);
			Assert.False(actual.StreamDeleted);
			Assert.Equal(new StreamPosition(1), actual.MetastreamRevision);
			Assert.Equal(expected.MaxCount, actual.Metadata.MaxCount);
			Assert.Equal(expected.MaxAge, actual.Metadata.MaxAge);
			Assert.Equal(expected.TruncateBefore, actual.Metadata.TruncateBefore);
			Assert.Equal(expected.CacheControl, actual.Metadata.CacheControl);
			Assert.Equal(expected.Acl, actual.Metadata.Acl);
		}
		
	
		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
