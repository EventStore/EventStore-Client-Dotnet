namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_permissions
	: IClassFixture<connect_to_existing_with_permissions.Fixture> {
	private const string Stream = nameof(connect_to_existing_with_permissions);

	private readonly Fixture _fixture;

	public connect_to_existing_with_permissions(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_succeeds() {
		await using var subscription =
			_fixture.Client.SubscribeToStream(Stream, "agroupname17", userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages
			.FirstAsync().AsTask().WithTimeout() is PersistentSubscriptionMessage.SubscriptionConfirmation);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() =>
			Client.CreateToStreamAsync(Stream, "agroupname17", new(), userCredentials: TestCredentials.Root);

		protected override Task When() => Task.CompletedTask;
	}
}
