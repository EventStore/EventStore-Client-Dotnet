using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests(
	ITestOutputHelper output,
	SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests.CustomFixture fixture
)
	: KurrentTemporaryTests<SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task connect_to_existing_without_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToStreamAsync(
					stream,
					group,
					delegate { return Task.CompletedTask; }
				);
			}
		).WithTimeout();
	}

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
