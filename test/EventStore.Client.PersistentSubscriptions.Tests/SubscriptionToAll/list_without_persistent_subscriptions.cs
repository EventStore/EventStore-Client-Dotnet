namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class list_without_persistent_subscriptions : IClassFixture<list_without_persistent_subscriptions.Fixture> {
	readonly Fixture _fixture;

	public list_without_persistent_subscriptions(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task throws() {
		if (SupportsPSToAll.No) {
			await Assert.ThrowsAsync<NotSupportedException>(async () => { await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.Root); });

			return;
		}

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () =>
				await _fixture.Client.ListToAllAsync(userCredentials: TestCredentials.Root)
		);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}