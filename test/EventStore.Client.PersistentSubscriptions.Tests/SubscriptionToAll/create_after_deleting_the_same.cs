namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class create_after_deleting_the_same : IClassFixture<create_after_deleting_the_same.Fixture> {
	readonly Fixture _fixture;

	public create_after_deleting_the_same(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_completion_succeeds() =>
		await _fixture.Client.CreateToAllAsync(
			"existing",
			new(),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override async Task When() {
			await Client.CreateToAllAsync(
				"existing",
				new(),
				userCredentials: TestCredentials.Root
			);

			await Client.DeleteToAllAsync(
				"existing",
				userCredentials: TestCredentials.Root
			);
		}
	}
}