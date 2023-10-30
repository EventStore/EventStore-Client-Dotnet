namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class create_on_non_existing_stream
	: IClassFixture<create_on_non_existing_stream.Fixture> {
	const    string  Stream = nameof(create_on_non_existing_stream);
	readonly Fixture _fixture;

	public create_on_non_existing_stream(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_completion_succeeds() =>
		await _fixture.Client.CreateToStreamAsync(
			Stream,
			"nonexistinggroup",
			new(),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}