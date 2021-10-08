using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class read_all_forward_messages : IClassFixture<read_all_forward_messages.Fixture> {
		private readonly Fixture _fixture;

		public read_all_forward_messages(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task stream_found() {
			var events = _fixture.CreateTestEvents(32).ToArray();

			var streamName = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

			var result = await _fixture.Client.ReadAllAsync(Direction.Forwards, Position.Start, 32,
				userCredentials: TestCredentials.Root).Messages.ToArrayAsync();

			Assert.Equal(33, result.Length);
			Assert.Equal(StreamMessage.Ok.Instance, result[0]);
			Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
