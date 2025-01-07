using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class SubscribeToStreamListTests(ITestOutputHelper output, SubscribeToStreamListTests.CustomFixture fixture)
	: KurrentTemporaryTests<SubscribeToStreamListTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task throws_with_no_credentials() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int streamSubscriptionCount = 4;

		for (var i = 0; i < streamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToStreamAsync(
				stream,
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		await Assert.ThrowsAsync<AccessDeniedException>(async () => await Fixture.Subscriptions.ListToStreamAsync(stream));
	}

	[RetryFact]
	public async Task throws_with_non_existing_user() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int streamSubscriptionCount = 4;

		for (var i = 0; i < streamSubscriptionCount; i++)
			await Fixture.Subscriptions.CreateToStreamAsync(
				stream,
				group + i,
				new(),
				userCredentials: TestCredentials.Root
			);

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () => await Fixture.Subscriptions.ListToStreamAsync(stream, userCredentials: TestCredentials.TestBadUser)
		);
	}

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
