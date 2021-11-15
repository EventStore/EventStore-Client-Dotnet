using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class read_stream_backward_messages : IClassFixture<read_stream_backward_messages.Fixture> {
		private readonly Fixture _fixture;

		public read_stream_backward_messages(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task stream_not_found() {
			var result = await _fixture.Client.ReadStreamAsync(Direction.Backwards, _fixture.GetStreamName(),
				StreamPosition.End).Messages.SingleAsync();

			Assert.Equal(StreamMessage.NotFound.Instance, result);
		}

		[Fact]
		public async Task stream_found() {
			var events = _fixture.CreateTestEvents(32).ToArray();

			var streamName = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

			var result = await _fixture.Client.ReadStreamAsync(Direction.Backwards, streamName,
				StreamPosition.End).Messages.ToArrayAsync();

			Assert.Equal(34, result.Length);
			Assert.Equal(StreamMessage.Ok.Instance, result[0]);
			Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
			Assert.Equal(new StreamMessage.LastStreamPosition(new StreamPosition(31)), result[^1]);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
