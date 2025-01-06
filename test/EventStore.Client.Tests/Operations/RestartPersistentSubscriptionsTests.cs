using EventStore.Client.Tests.TestNode;

namespace EventStore.Client.Tests.Operations;

public class RestartPersistentSubscriptionsTests(ITestOutputHelper output, RestartPersistentSubscriptionsTests.NoDefaultCredentialsFixture fixture)
	: KurrentTemporaryTests<RestartPersistentSubscriptionsTests.NoDefaultCredentialsFixture>(output, fixture) {
	[RetryFact]
	public async Task restart_persistent_subscriptions_does_not_throw() =>
		await Fixture.Operations
			.RestartPersistentSubscriptions(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task restart_persistent_subscriptions_without_credentials_throws() =>
		await Fixture.Operations
			.RestartPersistentSubscriptions()
			.ShouldThrowAsync<AccessDeniedException>();

	public class NoDefaultCredentialsFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
