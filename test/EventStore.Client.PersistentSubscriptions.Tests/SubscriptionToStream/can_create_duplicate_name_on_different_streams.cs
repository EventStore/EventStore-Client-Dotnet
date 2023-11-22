namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class can_create_duplicate_name_on_different_streams
	: IClassFixture<can_create_duplicate_name_on_different_streams.Fixture> {
	const string Stream =
		nameof(can_create_duplicate_name_on_different_streams);

	readonly Fixture _fixture;

	public can_create_duplicate_name_on_different_streams(Fixture fixture) => _fixture = fixture;

	[Fact]
	public Task the_completion_succeeds() =>
		_fixture.Client.CreateToStreamAsync(
			"someother" + Stream,
			"group3211",
			new(),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override Task When() =>
			Client.CreateToStreamAsync(
				Stream,
				"group3211",
				new(),
				userCredentials: TestCredentials.Root
			);
	}
}