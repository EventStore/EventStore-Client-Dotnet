namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class update_existing_filtered
	: IClassFixture<update_existing_filtered.Fixture> {
	const string Group = "existing-filtered";

	readonly Fixture _fixture;

	public update_existing_filtered(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_completion_succeeds() =>
		await _fixture.Client.UpdateToAllAsync(
			Group,
			new(true),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override async Task Given() =>
			await Client.CreateToAllAsync(
				Group,
				EventTypeFilter.Prefix("prefix-filter-"),
				new(),
				userCredentials: TestCredentials.Root
			);

		protected override Task When() => Task.CompletedTask;
	}
}