using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamGetInfoObsoleteTests(SubscribeToStreamGetInfoObsoleteTests.CustomFixture fixture)
	: IClassFixture<SubscribeToStreamGetInfoObsoleteTests.CustomFixture> {
	static readonly PersistentSubscriptionSettings Settings = new(
		true,
		StreamPosition.Start,
		true,
		TimeSpan.FromSeconds(9),
		11,
		303,
		30,
		909,
		TimeSpan.FromSeconds(1),
		1,
		1,
		500,
		SystemConsumerStrategies.RoundRobin
	);

	public static IEnumerable<object[]> AllowedUsers() {
		yield return new object[] { TestCredentials.Root };
	}

	[Theory]
	[MemberData(nameof(AllowedUsers))]
	public async Task returns_expected_result(UserCredentials credentials) {
		var result = await fixture.Subscriptions.GetInfoToStreamAsync(fixture.Stream, fixture.Group, userCredentials: credentials);

		Assert.Equal(fixture.Stream, result.EventSource);
		Assert.Equal(fixture.Group, result.GroupName);
		Assert.NotNull(Settings.StartFrom);
		Assert.True(result.Stats.TotalItems > 0);
		Assert.True(result.Stats.OutstandingMessagesCount > 0);
		Assert.True(result.Stats.AveragePerSecond >= 0);
		Assert.True(result.Stats.ParkedMessageCount >= 0);
		Assert.True(result.Stats.AveragePerSecond >= 0);
		Assert.True(result.Stats.ReadBufferCount >= 0);
		Assert.True(result.Stats.RetryBufferCount >= 0);
		Assert.True(result.Stats.CountSinceLastMeasurement >= 0);
		Assert.True(result.Stats.TotalInFlightMessages >= 0);
		Assert.NotNull(result.Stats.LastKnownEventPosition);
		Assert.NotNull(result.Stats.LastCheckpointedEventPosition);
		Assert.True(result.Stats.LiveBufferCount >= 0);

		Assert.NotNull(result.Connections);
		Assert.NotEmpty(result.Connections);
		var connection = result.Connections.First();
		Assert.NotNull(connection.From);
		Assert.Equal(TestCredentials.Root.Username, connection.Username);
		Assert.NotEmpty(connection.ConnectionName);
		Assert.True(connection.AverageItemsPerSecond >= 0);
		Assert.True(connection.TotalItems >= 0);
		Assert.True(connection.CountSinceLastMeasurement >= 0);
		Assert.True(connection.AvailableSlots >= 0);
		Assert.True(connection.InFlightMessages >= 0);
		Assert.NotNull(connection.ExtraStatistics);
		Assert.NotEmpty(connection.ExtraStatistics);

		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Highest);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Mean);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Median);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Fastest);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Quintile1);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Quintile2);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Quintile3);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Quintile4);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.Quintile5);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.NinetyPercent);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.NinetyFivePercent);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.NinetyNinePercent);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.NinetyNinePointFivePercent);
		AssertKeyAndValue(connection.ExtraStatistics, PersistentSubscriptionExtraStatistic.NinetyNinePointNinePercent);

		Assert.NotNull(result.Settings);
		Assert.Equal(Settings.StartFrom, result.Settings!.StartFrom);
		Assert.Equal(Settings.ResolveLinkTos, result.Settings!.ResolveLinkTos);
		Assert.Equal(Settings.ExtraStatistics, result.Settings!.ExtraStatistics);
		Assert.Equal(Settings.MessageTimeout, result.Settings!.MessageTimeout);
		Assert.Equal(Settings.MaxRetryCount, result.Settings!.MaxRetryCount);
		Assert.Equal(Settings.LiveBufferSize, result.Settings!.LiveBufferSize);
		Assert.Equal(Settings.ReadBatchSize, result.Settings!.ReadBatchSize);
		Assert.Equal(Settings.HistoryBufferSize, result.Settings!.HistoryBufferSize);
		Assert.Equal(Settings.CheckPointAfter, result.Settings!.CheckPointAfter);
		Assert.Equal(Settings.CheckPointLowerBound, result.Settings!.CheckPointLowerBound);
		Assert.Equal(Settings.CheckPointUpperBound, result.Settings!.CheckPointUpperBound);
		Assert.Equal(Settings.MaxSubscriberCount, result.Settings!.MaxSubscriberCount);
		Assert.Equal(Settings.ConsumerStrategyName, result.Settings!.ConsumerStrategyName);
	}

	[RetryFact]
	public async Task throws_when_given_non_existing_subscription() =>
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () => {
				await fixture.Subscriptions.GetInfoToStreamAsync(
					"NonExisting",
					"NonExisting",
					userCredentials: TestCredentials.Root
				);
			}
		);

	[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
	public async Task throws_with_non_existing_user() {
		var group  = $"NonExisting-{fixture.GetGroupName()}";
		var stream = $"NonExisting-{fixture.GetStreamName()}";

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () => {
				await fixture.Subscriptions.GetInfoToStreamAsync(
					stream,
					group,
					userCredentials: TestCredentials.TestBadUser
				);
			}
		);
	}

	[RetryFact]
	public async Task throws_with_no_credentials() {
		var group  = $"NonExisting-{fixture.GetGroupName()}";
		var stream = $"NonExisting-{fixture.GetStreamName()}";

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				await fixture.Subscriptions.GetInfoToStreamAsync(
					stream,
					group
				);
			}
		);
	}

	[RetryFact]
	public async Task returns_result_for_normal_user() {
		var result = await fixture.Subscriptions.GetInfoToStreamAsync(
			fixture.Stream,
			fixture.Group,
			userCredentials: TestCredentials.Root
		);

		Assert.NotNull(result);
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public string Group  { get; set; }
		public string Stream { get; set; }

		public CustomFixture() : base(x => x.WithoutDefaultCredentials()) {
			Group  = GetGroupName();
			Stream = GetStreamName();

			OnSetup += async () => {
				await Subscriptions.CreateToStreamAsync(
					groupName: Group,
					streamName: Stream,
					settings: Settings,
					userCredentials: TestCredentials.Root
				);

				var counter = 0;
				var tcs     = new TaskCompletionSource();

				await Subscriptions.SubscribeToStreamAsync(
					Stream,
					Group,
					(s, e, r, ct) => {
						counter++;

						if (counter == 1)
							s.Nack(PersistentSubscriptionNakEventAction.Park, "Test", e);

						if (counter > 10)
							tcs.TrySetResult();

						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root
				);

				for (var i = 0; i < 15; i++) {
					await Streams.AppendToStreamAsync(
						Stream,
						StreamState.Any,
						[new EventData(Uuid.NewUuid(), "test-event", ReadOnlyMemory<byte>.Empty)],
						userCredentials: TestCredentials.Root
					);
				}
			};
		}
	};

	void AssertKeyAndValue(IDictionary<string, long> items, string key) {
		Assert.True(items.ContainsKey(key));
		Assert.True(items[key] > 0);
	}
}
