using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllConnectWithoutReadPermissionsObsoleteTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_without_read_all_permissions() {
		var group = Fixture.GetGroupName();
		var user  = Fixture.GetUserCredentials();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Fixture.Users.CreateUserWithRetry(
			user.Username!,
			user.Username!,
			[],
			user.Password!,
			TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToAllAsync(
					group,
					delegate { return Task.CompletedTask; },
					userCredentials: user
				);
			}
		);
	}
}
