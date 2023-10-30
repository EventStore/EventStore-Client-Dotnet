namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_stream_backward_messages : IClassFixture<read_stream_backward_messages.Fixture> {
	readonly Fixture _fixture;

	public read_stream_backward_messages(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task stream_not_found() {
		var result = await _fixture.Client.ReadStreamAsync(
			Direction.Backwards,
			_fixture.GetStreamName(),
			StreamPosition.End
		).Messages.SingleAsync();

		Assert.Equal(StreamMessage.NotFound.Instance, result);
	}

	[Fact]
	public async Task stream_found() {
		var events = _fixture.CreateTestEvents(_fixture.Count).ToArray();

		var streamName = _fixture.GetStreamName();

		await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await _fixture.Client.ReadStreamAsync(
			Direction.Backwards,
			streamName,
			StreamPosition.End
		).Messages.ToArrayAsync();

		Assert.Equal(
			_fixture.Count + (_fixture.HasLastStreamPosition ? 2 : 1),
			result.Length
		);

		Assert.Equal(StreamMessage.Ok.Instance, result[0]);
		Assert.Equal(_fixture.Count, result.OfType<StreamMessage.Event>().Count());
		if (_fixture.HasLastStreamPosition)
			Assert.Equal(new StreamMessage.LastStreamPosition(new(31)), result[^1]);
	}

	public class Fixture : EventStoreClientFixture {
		public             int  Count                 => 32;
		public             bool HasLastStreamPosition => (EventStoreTestServer.Version?.Major ?? int.MaxValue) >= 21;
		protected override Task Given()               => Task.CompletedTask;
		protected override Task When()                => Task.CompletedTask;
	}
}