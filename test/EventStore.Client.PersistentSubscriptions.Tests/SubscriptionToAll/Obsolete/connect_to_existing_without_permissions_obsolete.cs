namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll.Obsolete;

[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class connect_to_existing_without_permissions_obsolete
	: IClassFixture<connect_to_existing_without_permissions_obsolete.Fixture> {
	readonly Fixture _fixture;
	public connect_to_existing_without_permissions_obsolete(Fixture fixture) => _fixture = fixture;

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
