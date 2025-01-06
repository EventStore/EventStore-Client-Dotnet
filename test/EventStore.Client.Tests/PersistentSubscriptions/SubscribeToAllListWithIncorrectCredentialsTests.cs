using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllListWithIncorrectCredentialsTests(ITestOutputHelper output, SubscribeToAllListWithIncorrectCredentialsTests.CustomFixture fixture)
	: KurrentTemporaryTests<SubscribeToAllListWithIncorrectCredentialsTests.CustomFixture>(output, fixture) {
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
