using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllWithoutPsTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task list_without_persistent_subscriptions() {
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () =>
				await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root)
		);
	}
}
