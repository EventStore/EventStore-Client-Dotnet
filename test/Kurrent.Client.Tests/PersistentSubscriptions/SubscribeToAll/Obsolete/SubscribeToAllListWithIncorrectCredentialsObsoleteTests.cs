using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllListWithIncorrectCredentialsObsoleteTests(ITestOutputHelper output, SubscribeToAllListWithIncorrectCredentialsObsoleteTests.CustomFixture fixture)
	: KurrentTemporaryTests<SubscribeToAllListWithIncorrectCredentialsObsoleteTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task throws_with_no_credentials() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		const int streamSubscriptionCount    = 4;
		const int allStreamSubscriptionCount = 3;

		for (var i = 0; i < streamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToStreamAsync(
				stream,
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		for (var i = 0; i < allStreamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToAllAsync(
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		await Assert.ThrowsAsync<AccessDeniedException>(async () => await Fixture.Subscriptions.ListToAllAsync());
	}

	[RetryFact]
	public async Task throws_with_non_existing_user() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		const int streamSubscriptionCount    = 4;
		const int allStreamSubscriptionCount = 3;

		for (var i = 0; i < streamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToStreamAsync(
				stream,
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		for (var i = 0; i < allStreamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToAllAsync(
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () => await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.TestBadUser)
		);
	}

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
