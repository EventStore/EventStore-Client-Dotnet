namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class deleting_without_permissions
	: IClassFixture<deleting_without_permissions.Fixture> {
	readonly Fixture _fixture;

	public deleting_without_permissions(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_delete_fails_with_access_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.Client.DeleteToAllAsync(Guid.NewGuid().ToString()));

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}