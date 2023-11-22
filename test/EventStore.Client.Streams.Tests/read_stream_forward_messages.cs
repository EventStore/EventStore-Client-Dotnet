namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_stream_forward_messages : IClassFixture<read_stream_forward_messages.Fixture> {
	readonly Fixture _fixture;

	public read_stream_forward_messages(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task stream_not_found() {
		var result = await _fixture.Client.ReadStreamAsync(
			Direction.Forwards,
			_fixture.GetStreamName(),
			StreamPosition.Start
		).Messages.SingleAsync();

		Assert.Equal(StreamMessage.NotFound.Instance, result);
	}

	[Fact]
	public async Task stream_found() {
		var events = _fixture.CreateTestEvents(_fixture.Count).ToArray();

		var streamName = _fixture.GetStreamName();

		await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await _fixture.Client.ReadStreamAsync(
			Direction.Forwards,
			streamName,
			StreamPosition.Start
		).Messages.ToArrayAsync();

		Assert.Equal(
			_fixture.Count + (_fixture.HasStreamPositions ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(_fixture.Count, result.OfType<StreamMessage.Event>().Count());
		var first = Assert.IsType<StreamMessage.Event>(result[1]);
		Assert.Equal(new(0), first.ResolvedEvent.OriginalEventNumber);
		var last = Assert.IsType<StreamMessage.Event>(result[_fixture.HasStreamPositions ? ^2 : ^1]);
		Assert.Equal(new((ulong)_fixture.Count - 1), last.ResolvedEvent.OriginalEventNumber);

		if (_fixture.HasStreamPositions)
			if (_fixture.HasStreamPositions)
				Assert.Equal(
					new StreamMessage.LastStreamPosition(new((ulong)_fixture.Count - 1)),
					result[^1]
				);
	}

	[Fact]
	public async Task stream_found_truncated() {
		var events = _fixture.CreateTestEvents(_fixture.Count).ToArray();

		var streamName = _fixture.GetStreamName();

		await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		await _fixture.Client.SetStreamMetadataAsync(
			streamName,
			StreamState.Any,
			new(truncateBefore: new StreamPosition(32))
		);

		var result = await _fixture.Client.ReadStreamAsync(
			Direction.Forwards,
			streamName,
			StreamPosition.Start
		).Messages.ToArrayAsync();

		Assert.Equal(
			_fixture.Count - 32 + (_fixture.HasStreamPositions ? 3 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);

		if (_fixture.HasStreamPositions)
			Assert.Equal(new StreamMessage.FirstStreamPosition(new(32)), result[1]);

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());

		if (_fixture.HasStreamPositions)
			Assert.Equal(
				new StreamMessage.LastStreamPosition(new((ulong)_fixture.Count - 1)),
				result[^1]
			);
	}

	public class Fixture : EventStoreClientFixture {
		public             int  Count              => 64;
		public             bool HasStreamPositions => (EventStoreTestServer.Version?.Major ?? int.MaxValue) >= 21;
		protected override Task Given()            => Task.CompletedTask;
		protected override Task When()             => Task.CompletedTask;
	}
}