using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllReturnsAllSubscriptions(ITestOutputHelper output, SubscribeToAllReturnsAllSubscriptions.CustomFixture fixture)
	: KurrentTemporaryTests<SubscribeToAllReturnsAllSubscriptions.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task returns_all_subscriptions() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		const int streamSubscriptionCount    = 4;
		const int allStreamSubscriptionCount = 3;
		const int totalSubscriptionCount     = streamSubscriptionCount + allStreamSubscriptionCount;

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

		var result = (await Fixture.Subscriptions.ListAllAsync(userCredentials: TestCredentials.Root)).ToList();
		Assert.Equal(totalSubscriptionCount, result.Count);
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() {
			SkipPsWarmUp = true;
		}
	}
}
