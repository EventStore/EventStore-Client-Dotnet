using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client.Regressions {
	public class Issue_2678  : IClassFixture<Issue_2678.Fixture> {
		private readonly Fixture _fixture;

		public Issue_2678(Fixture fixture, ITestOutputHelper outputHelper) {
			_fixture = fixture;
			_fixture.CaptureLogs(outputHelper);
		}
		[Fact]
		public async Task sequence() {
			var streamName = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, _fixture.CreateTestEvents());

			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(() =>
				_fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, _fixture.CreateTestEvents()));

			Assert.Equal(StreamState.NoStream.ToInt64(), ex.ExpectedVersion);
		}
		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
