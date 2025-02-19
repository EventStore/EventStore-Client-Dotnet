using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Operations;

[Trait("Category", "Target:Operations")]
public class RestartPersistentSubscriptionsTests(ITestOutputHelper output, RestartPersistentSubscriptionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<RestartPersistentSubscriptionsTests.CustomFixture>(output, fixture) {
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

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
