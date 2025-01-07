using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllResultWithNormalUserCredentialsTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task returns_result_with_normal_user_credentials() {
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

		var result = await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root);
		Assert.Equal(allStreamSubscriptionCount, result.Count());
	}
}
