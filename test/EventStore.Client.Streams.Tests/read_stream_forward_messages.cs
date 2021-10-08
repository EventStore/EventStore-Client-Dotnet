using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class read_stream_forward_messages : IClassFixture<read_stream_forward_messages.Fixture> {
		private readonly Fixture _fixture;

		public read_stream_forward_messages(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task stream_not_found() {
			var result = await _fixture.Client.ReadStreamAsync(Direction.Forwards, _fixture.GetStreamName(),
				StreamPosition.Start).Messages.SingleAsync();

			Assert.Equal(StreamMessage.NotFound.Instance, result);
		}

		[Fact]
		public async Task stream_found() {
			var events = _fixture.CreateTestEvents(32).ToArray();

			var streamName = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

			var result = await _fixture.Client.ReadStreamAsync(Direction.Forwards, streamName,
				StreamPosition.Start).Messages.ToArrayAsync();

			Assert.Equal(34, result.Length);
			Assert.Equal(StreamMessage.Ok.Instance, result[0]);
			Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
			var first = Assert.IsType<StreamMessage.Event>(result[1]);
			Assert.Equal(new StreamPosition(0), first.ResolvedEvent.OriginalEventNumber);
			var last = Assert.IsType<StreamMessage.Event>(result[^2]);
			Assert.Equal(new StreamPosition(31), last.ResolvedEvent.OriginalEventNumber);
			Assert.Equal(new StreamMessage.LastStreamPosition(new StreamPosition(31)), result[^1]);
		}

		[Fact]
		public async Task stream_found_truncated() {
			var events = _fixture.CreateTestEvents(64).ToArray();

			var streamName = _fixture.GetStreamName();

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

			await _fixture.Client.SetStreamMetadataAsync(streamName, StreamState.Any,
				new StreamMetadata(truncateBefore: new StreamPosition(32)));

			var result = await _fixture.Client.ReadStreamAsync(Direction.Forwards, streamName,
				StreamPosition.Start).Messages.ToArrayAsync();

			Assert.Equal(35, result.Length);
			Assert.Equal(StreamMessage.Ok.Instance, result[0]);
			Assert.Equal(new StreamMessage.FirstStreamPosition(new StreamPosition(32)), result[1]);
			Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
			Assert.Equal(new StreamMessage.LastStreamPosition(new StreamPosition(63)), result[^1]);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
