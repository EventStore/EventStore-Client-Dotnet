namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class deleting_nonexistent
	: IClassFixture<deleting_nonexistent.Fixture> {
	readonly Fixture _fixture;

	public deleting_nonexistent(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_delete_fails_with_argument_exception() =>
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() => _fixture.Client.DeleteToAllAsync(Guid.NewGuid().ToString(), userCredentials: TestCredentials.Root)
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}