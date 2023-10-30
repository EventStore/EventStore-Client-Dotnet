namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class connect_to_existing_with_max_one_client
	: IClassFixture<connect_to_existing_with_max_one_client.Fixture> {
	const    string  Group  = "startinbeginning1";
	const    string  Stream = nameof(connect_to_existing_with_max_one_client);
	readonly Fixture _fixture;

	public connect_to_existing_with_max_one_client(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_second_subscription_fails_to_connect() {
		using var first = await _fixture.Client.SubscribeToStreamAsync(
			Stream,
			Group,
			delegate { return Task.CompletedTask; },
			userCredentials: TestCredentials.Root
		).WithTimeout();

		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(
			async () => {
				using var _ = await _fixture.Client.SubscribeToStreamAsync(
					Stream,
					Group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root
				);
			}
		).WithTimeout();

		Assert.Equal(Stream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() =>
			Client.CreateToStreamAsync(
				Stream,
				Group,
				new(maxSubscriberCount: 1),
				userCredentials: TestCredentials.Root
			);

		protected override Task When() => Task.CompletedTask;
	}
}