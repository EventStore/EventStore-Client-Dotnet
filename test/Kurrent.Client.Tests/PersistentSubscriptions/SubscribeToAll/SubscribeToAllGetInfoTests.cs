// ReSharper disable InconsistentNaming

using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllGetInfoTests(SubscribeToAllGetInfoTests.CustomFixture fixture)
	: IClassFixture<SubscribeToAllGetInfoTests.CustomFixture> {
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

				foreach (var eventData in CreateTestEvents(20)) {
					await Streams.AppendToStreamAsync(
						$"test-{Guid.NewGuid():n}",
						StreamState.NoStream,
						[eventData],
						userCredentials: TestCredentials.Root
					);
				}

				var counter = 0;

				await using var subscription = Subscriptions.SubscribeToAll(Group, userCredentials: TestCredentials.Root);

				var enumerator = subscription.Messages.GetAsyncEnumerator();

				while (await enumerator.MoveNextAsync()) {
					if (enumerator.Current is not PersistentSubscriptionMessage.Event (var resolvedEvent, _))
						continue;

					counter++;

					if (counter == 1)
						await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Test", resolvedEvent);

					if (counter > 10)
						return;
				}
			};
		}
	};
}
