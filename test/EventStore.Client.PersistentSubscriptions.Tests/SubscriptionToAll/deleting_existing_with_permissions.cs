namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class deleting_existing_with_permissions
	: IClassFixture<deleting_existing_with_permissions.Fixture> {
	readonly Fixture _fixture;

	public deleting_existing_with_permissions(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public Task the_delete_of_group_succeeds() =>
		_fixture.Client.DeleteToAllAsync(
			"groupname123",
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override Task When() =>
			Client.CreateToAllAsync(
				"groupname123",
				new(),
				userCredentials: TestCredentials.Root
			);
	}
}