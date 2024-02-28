namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class update_existing_without_permissions
	: IClassFixture<update_existing_without_permissions.Fixture> {
	const    string  Stream = nameof(update_existing_without_permissions);
	const    string  Group  = "existing";
	readonly Fixture _fixture;

	public update_existing_without_permissions(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task the_completion_fails_with_access_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(
			() => _fixture.Client.UpdateToStreamAsync(
				Stream,
				Group,
				new()
			)
		);

	public class Fixture : EventStoreClientFixture {
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(
				Stream,
				StreamState.NoStream,
				CreateTestEvents(),
				userCredentials: TestCredentials.Root
			);

			await Client.CreateToStreamAsync(
				Stream,
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);
		}

		protected override Task When() => Task.CompletedTask;
	}
}