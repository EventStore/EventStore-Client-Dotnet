// ReSharper disable InconsistentNaming

using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllGetInfoObsoleteTests(SubscribeToAllGetInfoObsoleteTests.CustomFixture fixture)
	: IClassFixture<SubscribeToAllGetInfoObsoleteTests.CustomFixture> {
	static readonly PersistentSubscriptionSettings Settings = new(
		resolveLinkTos: true,
		startFrom: Position.Start,
		extraStatistics: true,
		messageTimeout: TimeSpan.FromSeconds(9),
		maxRetryCount: 11,
		liveBufferSize: 303,
		readBatchSize: 30,
		historyBufferSize: 909,
		checkPointAfter: TimeSpan.FromSeconds(1),
		checkPointLowerBound: 1,
		checkPointUpperBound: 1,
		maxSubscriberCount: 500,
		consumerStrategyName: SystemConsumerStrategies.Pinned
	);

	[RetryFact]
	public async Task throws_with_non_existing_subscription() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () => await fixture.Subscriptions.GetInfoToAllAsync(group, userCredentials: TestCredentials.Root)
		);
	}

	[RetryFact]
	public async Task throws_with_no_credentials() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () =>
				await fixture.Subscriptions.GetInfoToAllAsync(group)
		);
	}

	[RetryFact]
	public async Task throws_with_non_existing_user() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () =>
				await fixture.Subscriptions.GetInfoToAllAsync(group, userCredentials: TestCredentials.TestBadUser)
		);
	}

	[RetryFact]
	public async Task returns_result_with_normal_user_credentials() {
		var result = await fixture.Subscriptions.GetInfoToAllAsync(fixture.Group, userCredentials: TestCredentials.Root);

		Assert.Equal("$all", result.EventSource);
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public string Group { get; }

		public CustomFixture() : base(x => x.WithoutDefaultCredentials()) {
			Group = GetGroupName();

			OnSetup += async () => {
				await Subscriptions.CreateToAllAsync(Group, Settings, userCredentials: TestCredentials.Root);

				var counter = 0;
				var tcs     = new TaskCompletionSource();

				await Subscriptions.SubscribeToAllAsync(
					Group,
					(s, e, r, ct) => {
						counter++;

						switch (counter) {
							case 1:
								s.Nack(PersistentSubscriptionNakEventAction.Park, "Test", e);
								break;

							case > 10:
								tcs.TrySetResult();
								break;
						}

						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root
				);
			};
		}
	};
}
