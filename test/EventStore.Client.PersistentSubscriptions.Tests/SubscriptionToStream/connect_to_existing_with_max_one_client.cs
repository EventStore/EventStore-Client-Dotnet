namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_max_one_client
	: IClassFixture<connect_to_existing_with_max_one_client.Fixture> {
	private const string Group = "startinbeginning1";
	private const string Stream = nameof(connect_to_existing_with_max_one_client);
	private readonly Fixture _fixture;

	public connect_to_existing_with_max_one_client(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_second_subscription_fails_to_connect() {
		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(
			() => Task.WhenAll(Subscribe().WithTimeout(), Subscribe().WithTimeout()));

		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
		return;

		async Task Subscribe() {
			await using var subscription =
				_fixture.Client.SubscribeToStream(Stream, Group, userCredentials: TestCredentials.Root);
			await subscription.Messages.AnyAsync();
		}
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Client.CreateToStreamAsync(Stream, Group, new(maxSubscriberCount: 1),
			userCredentials: TestCredentials.Root);

		protected override Task When() => Task.CompletedTask;
	}
}
