namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_non_existing_with_permissions
	: IClassFixture<connect_to_non_existing_with_permissions.Fixture> {
	private const string Group = "foo";

	private readonly Fixture _fixture;

	public connect_to_non_existing_with_permissions(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task throws_persistent_subscription_not_found() {
		await using var subscription = _fixture.Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
			.AsTask()
			.WithTimeout());
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When() => Task.CompletedTask;
	}
}
