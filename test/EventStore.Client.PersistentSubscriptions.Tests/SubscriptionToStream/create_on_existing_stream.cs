namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class create_on_existing_stream
	: IClassFixture<create_on_existing_stream.Fixture> {
	const    string  Stream = nameof(create_on_existing_stream);
	readonly Fixture _fixture;

	public create_on_existing_stream(Fixture fixture) => _fixture = fixture;

	[Fact]
	public Task the_completion_succeeds() =>
		_fixture.Client.CreateToStreamAsync(
			Stream,
			"existing",
			new(),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override async Task When() => await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents());
	}
}