using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllReturnsSubscriptionsToAllStreamTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task returns_subscriptions_to_all_stream() {
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

		var result = (await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root)).ToList();
		Assert.Equal(allStreamSubscriptionCount, result.Count);
		Assert.All(result, s => Assert.Equal("$all", s.EventSource));
	}
}
