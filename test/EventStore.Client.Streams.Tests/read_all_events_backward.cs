using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	[Trait("Category", "LongRunning")]
	public class read_all_events_backward : IClassFixture<read_all_events_backward.Fixture> {
		private const string Stream = "stream";
		private readonly Fixture _fixture;

		public read_all_events_backward(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task return_empty_if_reading_from_start() {
			var count = await _fixture.Client.ReadAllAsync(Direction.Backwards, Position.Start, 1).CountAsync();
			Assert.Equal(0, count);
		}

		[Fact]
		public async Task return_partial_slice_if_not_enough_events() {
			var events = await _fixture.Client.ReadAllAsync(Direction.Backwards, Position.End, _fixture.Events.Length * 2)
				.ToArrayAsync();

			Assert.True(events.Length < _fixture.Events.Length * 2);
		}

		[Fact]
		public async Task return_events_in_reversed_order_compared_to_written() {
			var events = await _fixture.Client.ReadAllAsync(Direction.Backwards, Position.End, _fixture.Events.Length)
				.ToArrayAsync();

			Assert.True(EventDataComparer.Equal(
				_fixture.Events.Reverse().ToArray(),
				events.AsResolvedTestEvents().ToArray()));
		}

		[Fact]
		public async Task return_single_event() {
			var events = await _fixture.Client.ReadAllAsync(Direction.Backwards, Position.End, maxCount: 1)
				.ToArrayAsync();

			var actualEvent = Assert.Single(events.AsResolvedTestEvents());
			Assert.True(EventDataComparer.Equal(
				_fixture.Events.Last(),
				actualEvent));
		}

		[Fact(Skip = "Not Implemented")]
		public Task be_able_to_read_all_one_by_one_until_end_of_stream() {
			throw new NotImplementedException();
		}

		[Fact(Skip = "Not Implemented")]
		public Task be_able_to_read_events_slice_at_time() {
			throw new NotImplementedException();
		}

		[Fact(Skip = "Not Implemented")]
		public Task when_got_int_max_value_as_maxcount_should_throw() {
			throw new NotImplementedException();
		}

		[Fact]
		public async Task max_count_is_respected() {
			var maxCount = _fixture.Events.Length / 2;
			var events = await _fixture.Client.ReadAllAsync(Direction.Backwards, Position.End, maxCount)
				.Take(_fixture.Events.Length)
				.ToArrayAsync();

			Assert.Equal(maxCount, events.Length);
		}

		public class Fixture : EventStoreClientFixture {
			public EventData[] Events { get; }

			public Fixture() {
				Events = Enumerable
					.Concat(
						CreateTestEvents(count: 20),
						CreateTestEvents(count: 2, metadataSize: 1_000_000))
					.ToArray();
			}
			protected override async Task Given() {
				var result = await Client.SetStreamMetadataAsync(
					SystemStreams.AllStream,
					StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(readRole: SystemRoles.All)),
					userCredentials: TestCredentials.Root);

				await Client.AppendToStreamAsync(Stream, StreamState.NoStream, Events);
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
