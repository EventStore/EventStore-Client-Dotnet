namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_all_backward_messages : IClassFixture<read_all_backward_messages.Fixture> {
	readonly Fixture _fixture;

	public read_all_backward_messages(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task stream_found() {
		var events = _fixture.CreateTestEvents(32).ToArray();

		var streamName = _fixture.GetStreamName();

		await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream, events);

		var result = await _fixture.Client.ReadAllAsync(
			Direction.Backwards,
			Position.End,
			32,
			userCredentials: TestCredentials.Root
		).Messages.ToArrayAsync();

		Assert.Equal(32, result.OfType<StreamMessage.Event>().Count());
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}