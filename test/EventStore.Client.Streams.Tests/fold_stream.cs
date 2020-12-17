using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace EventStore.Client {
	[Trait("Category", "Network")]
	public class fold_stream : IClassFixture<fold_stream.Fixture> {
		private readonly Fixture _fixture;

		public fold_stream(Fixture fixture) {
			_fixture = fixture;
		}


		[Fact]
		public async Task fold_is_called_for_each_event() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => { acc.Add(e); return acc; },
				streamName,
				StreamPosition.Start,
				new List<StreamPosition>());

			var expected =
				Enumerable.Range(0, count)
				.Select(p => new StreamPosition((ulong)p));

			Assert.Equal(expected, result.Value);
		}

		[Fact]
		public async Task fold_result_revision_is_last_event() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => acc + 1,
				streamName,
				StreamPosition.Start,
				0);



			Assert.Equal(StreamRevision.FromInt64 (count-1), result.Revision);
		}


		[Fact]
		public async Task fold_result_revision_is_none_when_stream_doesnt_exist() {
			var streamName = _fixture.GetStreamName();

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => acc + 1,
				streamName,
				StreamPosition.Start,
				0);

			Assert.Equal(StreamRevision.None, result.Revision);
		}

		[Fact]
		public async Task fold_result_value_is_seed_when_stream_doesnt_exist() {
			var streamName = _fixture.GetStreamName();

			var seed = new object();
			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => new object(),
				streamName,
				StreamPosition.Start,
				seed);

			Assert.Same(seed, result.Value);
		}


		[Fact]
		public async Task fold_result_is_seed_if_all_events_are_deserialized_to_empty() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var seed = new object();
			var result = await _fixture.Client.FoldStreamAsync(
				e => Array.Empty<int>(),
				(acc, e) => new object(),
				streamName,
				StreamPosition.Start,
				seed);



			Assert.Same(seed, result.Value);
		}


		[Fact]
		public async Task fold_starting_after_last_event_has_position() {
			var streamName = _fixture.GetStreamName();
			const int pos = 20;
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => { acc.Add(e); return acc; },
				streamName,
				StreamPosition.FromInt64(pos),
				new List<StreamPosition>());


			var expected = StreamRevision.FromInt64(pos-1);
			Assert.Equal(expected, result.Revision);
		}


		[Fact]
		public async Task fold_starting_from_position_contains_expected_events () {
			var streamName = _fixture.GetStreamName();
			const int pos = 10;
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e.Event.EventNumber },
				(acc, e) => { acc.Add(e); return acc; },
				streamName,
				StreamPosition.FromInt64(pos),
				new List<StreamPosition>());


			var expected =
					Enumerable.Range(pos, count-pos)
					.Select(p => new StreamPosition((ulong)p));

			Assert.Equal(expected, result.Value);
		}

		[Fact]
		public async Task fold_starting_from_start_contains_expected_events() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { e },
				(acc, e) => { acc.Add(e.Event.EventNumber); return acc; },
				streamName,
				StreamPosition.Start,
				new List<StreamPosition>());


			var expected =
					Enumerable.Range(0, count)
					.Select(p => new StreamPosition((ulong)p));

			Assert.Equal(expected, result.Value);
		}


		[Fact]
		public async Task fold_with_deserializer_returning_multiple_events_process_them_in_order() {
			var streamName = _fixture.GetStreamName();
			const int count = 20;

			await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
				_fixture.CreateTestEvents(count));

			var result = await _fixture.Client.FoldStreamAsync(
				e => new[] { (e.Event.EventNumber, 0 ), ( e.Event.EventNumber, 1 ) },
				(acc, e) => { acc.Add(e); return acc; },
				streamName,
				StreamPosition.Start,
				new List<(StreamPosition,int)>());


			var expected =
					from p in Enumerable.Range(0, count)
					from id in Enumerable.Range(0,2)
					select (new StreamPosition((ulong)p), id);

			Assert.Equal(expected, result.Value);
		}


		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
