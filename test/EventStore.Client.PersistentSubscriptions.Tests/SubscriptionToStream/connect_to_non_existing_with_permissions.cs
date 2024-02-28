namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_non_existing_with_permissions : IClassFixture<connect_to_non_existing_with_permissions.Fixture> {
	private const string Stream = nameof(connect_to_non_existing_with_permissions);
	private const string Group  = "foo";

	private readonly Fixture _fixture;

	public connect_to_non_existing_with_permissions(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task throws_persistent_subscription_not_found() {
		await using var subscription = _fixture.Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.Root);
		Assert.True(await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>()
			.AnyAsync()
			.AsTask()
			.WithTimeout());
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}
