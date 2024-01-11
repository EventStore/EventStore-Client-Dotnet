namespace EventStore.Client.Operations.Tests;

public class RestartPersistentSubscriptionsTests : IClassFixture<InsecureClientTestFixture> {
	public RestartPersistentSubscriptionsTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }

	[Fact]
	public async Task restart_persistent_subscriptions_does_not_throw() =>
		await Fixture.Operations
			.RestartPersistentSubscriptions(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[Fact]
	public async Task restart_persistent_subscriptions_without_credentials_throws() =>
		await Fixture.Operations
			.RestartPersistentSubscriptions()
			.ShouldThrowAsync<AccessDeniedException>();
}