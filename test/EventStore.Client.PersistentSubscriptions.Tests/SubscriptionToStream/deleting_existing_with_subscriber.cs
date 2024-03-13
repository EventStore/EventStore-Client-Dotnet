namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class deleting_existing_with_subscriber : IClassFixture<deleting_existing_with_subscriber.Fixture> {
	private const string Stream = nameof(deleting_existing_with_subscriber);
	private readonly Fixture _fixture;

	public deleting_existing_with_subscriber(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_subscription_is_dropped_with_not_found() {
		await using var subscription = _fixture.Client.SubscribeToStream(Stream, "groupname123", userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
			.AsTask()
			.WithTimeout());
	}

	public class Fixture : EventStoreClientFixture {
		protected override async Task Given() {
			await Client.CreateToStreamAsync(Stream, "groupname123", new(), userCredentials: TestCredentials.Root);
		}

		protected override Task When() =>
			Client.DeleteToStreamAsync(Stream, "groupname123", userCredentials: TestCredentials.Root);
	}
}
