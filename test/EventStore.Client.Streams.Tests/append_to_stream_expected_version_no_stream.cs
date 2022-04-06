using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class append_to_stream_expected_version_no_stream :
		IClassFixture<append_to_stream_expected_version_no_stream.Fixture> {
		private readonly Fixture _fixture;

		public append_to_stream_expected_version_no_stream(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public void succeeds() {
			Assert.Equal(new StreamRevision(0), _fixture.Result!.NextExpectedStreamRevision);
		}

		[Fact]
		public void returns_position() {
			Assert.True(_fixture.Result!.LogPosition > Position.Start);
		}

		public class Fixture : EventStoreClientFixture {
			public IWriteResult? Result { get; private set; }

			protected override Task Given() => Task.CompletedTask;

			protected override async Task When() {
				Result = await Client.AppendToStreamAsync("stream-1", StreamState.NoStream,
					CreateTestEvents());
			}
		}
	}
}
