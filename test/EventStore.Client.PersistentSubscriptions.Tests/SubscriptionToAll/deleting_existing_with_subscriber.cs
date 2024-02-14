namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class deleting_existing_with_subscriber : IClassFixture<deleting_existing_with_subscriber.Fixture> {
	public const string Group = "groupname123";
	private readonly Fixture _fixture;

	public deleting_existing_with_subscriber(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_subscription_is_dropped_with_not_found() {
		await using var subscription = _fixture.Client.SubscribeToAll(Group, userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
			.AsTask()
			.WithTimeout());
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Client.CreateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);

		protected override Task When() => Client.DeleteToAllAsync(Group, userCredentials: TestCredentials.Root);
	}
}
