using System.Threading.Tasks;
using EventStore.Tests.Fixtures;

namespace EventStore.Client {
	public class append_to_stream_expected_version_no_stream : IClassFixture<EventStoreClientIntegrationFixture> {
		readonly EventStoreClientIntegrationFixture _fixture;

		public append_to_stream_expected_version_no_stream(EventStoreClientIntegrationFixture fixture) => _fixture = fixture;

		[Fact]
		public async Task succeeds() {
			var result = await _fixture.Client.AppendToStreamAsync("stream-1", StreamState.NoStream, _fixture.CreateTestEvents());
			Assert.Equal(new StreamRevision(0), result.NextExpectedStreamRevision);
		}

		[Fact]
		public async Task returns_position() {
			var result = await _fixture.Client.AppendToStreamAsync("stream-2", StreamState.NoStream, _fixture.CreateTestEvents());
			Assert.True(result.LogPosition > Position.Start);
		}
	}
}
