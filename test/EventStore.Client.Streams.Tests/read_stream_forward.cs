using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class read_stream_forward : IClassFixture<read_stream_forward.Fixture> {
		private readonly Fixture _fixture;

		public read_stream_forward(Fixture fixture) {
			_fixture = fixture;
		}

		[Theory, InlineData(0)]
		public async Task count_le_equal_zero_throws(ulong count) {
			var stream = _fixture.GetStreamName();

			var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
				_fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, count)
					.ToArrayAsync().AsTask());

			Assert.Equal(nameof(count), ex.ParamName);
		}

		[Fact]
		public async Task stream_does_not_exist_throws() {
			var stream = _fixture.GetStreamName();

			var ex = await Assert.ThrowsAsync<StreamNotFoundException>(() => _fixture.Client
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.ToArrayAsync().AsTask());

			Assert.Equal(stream, ex.Stream);
		}
		
		[Fact]
		public async Task stream_does_not_exist_can_be_checked() {
			var stream = _fixture.GetStreamName();

			var result = _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1);
			
			var state = await result.ReadState;
			Assert.Equal(ReadState.StreamNotFound, state);
		}
		
		[Fact]
		public async Task stream_deleted_throws() {
			var stream = _fixture.GetStreamName();

			await _fixture.Client.TombstoneAsync(stream, StreamState.NoStream);

			var ex = await Assert.ThrowsAsync<StreamDeletedException>(() => _fixture.Client
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.ToArrayAsync().AsTask());

			Assert.Equal(stream, ex.Stream);
		}

		[Fact]
		public async Task returns_events_in_order() {
			var stream = _fixture.GetStreamName();

			var expected = _fixture.CreateTestEvents(10).ToArray();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, expected);

			var actual = await _fixture.Client
				.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, (ulong)expected.Length)
				.Select(x => x.Event).ToArrayAsync();

			Assert.True(EventDataComparer.Equal(expected, actual));
		}

		[Fact]
		public async Task be_able_to_read_single_event_from_arbitrary_position() {
			var stream = _fixture.GetStreamName();

			var events = _fixture.CreateTestEvents(10).ToArray();

			var expected = events[7];

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, events);

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, new StreamPosition(7), 1)
				.Select(x => x.Event)
				.SingleAsync();

			Assert.True(EventDataComparer.Equal(expected, actual));
		}

		[Fact]
		public async Task be_able_to_read_from_arbitrary_position() {
			var stream = _fixture.GetStreamName();

			var events = _fixture.CreateTestEvents(10).ToArray();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, events);

			var actual = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, new StreamPosition(3), 2)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.True(EventDataComparer.Equal(events.Skip(3).Take(2).ToArray(), actual));
		}

		[Fact]
		public async Task be_able_to_read_first_event() {
			var stream = _fixture.GetStreamName();

			var testEvents = _fixture.CreateTestEvents(10).ToArray();

			await _fixture.Client.AppendToStreamAsync(stream, StreamState.NoStream, testEvents);

			var events = await _fixture.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 1)
				.Select(x => x.Event)
				.ToArrayAsync();

			Assert.Single(events);
			Assert.True(EventDataComparer.Equal(testEvents[0], events[0]));
		}

		[Fact]
		public async Task max_count_is_respected() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;
			const ulong maxCount = (ulong)count / 2;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var events = await _fixture.Client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start, maxCount)
				.Take(count)
				.ToArrayAsync();

			Assert.Equal(maxCount, (ulong)events.Length);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
