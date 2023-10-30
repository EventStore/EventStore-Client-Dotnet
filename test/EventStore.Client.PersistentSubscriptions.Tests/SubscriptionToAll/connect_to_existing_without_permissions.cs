namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class connect_to_existing_without_permissions
	: IClassFixture<connect_to_existing_without_permissions.Fixture> {
	readonly Fixture _fixture;
	public connect_to_existing_without_permissions(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public Task throws_access_denied() =>
		Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				using var _ = await _fixture.Client.SubscribeToAllAsync(
					"agroupname55",
					delegate { return Task.CompletedTask; }
				);
			}
		).WithTimeout();

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override Task Given() =>
			Client.CreateToAllAsync(
				"agroupname55",
				new(),
				userCredentials: TestCredentials.Root
			);

		protected override Task When() => Task.CompletedTask;
	}
}