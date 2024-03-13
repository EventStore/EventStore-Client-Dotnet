namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_with_permissions : IClassFixture<connect_to_existing_with_permissions.Fixture> {
	private const string Group = "connectwithpermissions";
	private readonly Fixture _fixture;

	public connect_to_existing_with_permissions(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_succeeds() {
		await using var subscription =
			_fixture.Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages
			.FirstAsync().AsTask().WithTimeout() is PersistentSubscriptionMessage.SubscriptionConfirmation);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Client.CreateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);

		protected override Task When() => Task.CompletedTask;
	}
}
