namespace EventStore.Client.SubscriptionToAll;

public class get_info : IClassFixture<get_info.Fixture> {
    const string GroupName = nameof(get_info);

    static readonly PersistentSubscriptionSettings _settings = new(
        true,
        Position.Start,
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
        SystemConsumerStrategies.Pinned
    );

    readonly Fixture _fixture;

    public get_info(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task throws_when_not_supported() {
        if (SupportsPSToAll.No)
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => { await _fixture.Client.GetInfoToAllAsync(GroupName, userCredentials: TestCredentials.Root); }
            );
    }

    [SupportsPSToAll.Fact]
    public async Task returns_expected_result() {
        var result = await _fixture.Client.GetInfoToAllAsync(GroupName, userCredentials: TestCredentials.Root);

        Assert.Equal("$all", result.EventSource);
        Assert.Equal(GroupName, result.GroupName);
        Assert.Equal("Live", result.Status);

        Assert.NotNull(_settings.StartFrom);
        Assert.True(result.Stats.TotalItems > 0);
        Assert.True(result.Stats.OutstandingMessagesCount > 0);
        Assert.True(result.Stats.AveragePerSecond >= 0);
        Assert.True(result.Stats.ParkedMessageCount > 0);
        Assert.True(result.Stats.AveragePerSecond >= 0);
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
        Assert.True(connection.TotalItems > 0);
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
        Assert.Equal(_settings.StartFrom, result.Settings!.StartFrom);
        Assert.Equal(_settings.ResolveLinkTos, result.Settings!.ResolveLinkTos);
        Assert.Equal(_settings.ExtraStatistics, result.Settings!.ExtraStatistics);
        Assert.Equal(_settings.MessageTimeout, result.Settings!.MessageTimeout);
        Assert.Equal(_settings.MaxRetryCount, result.Settings!.MaxRetryCount);
        Assert.Equal(_settings.LiveBufferSize, result.Settings!.LiveBufferSize);
        Assert.Equal(_settings.ReadBatchSize, result.Settings!.ReadBatchSize);
        Assert.Equal(_settings.HistoryBufferSize, result.Settings!.HistoryBufferSize);
        Assert.Equal(_settings.CheckPointAfter, result.Settings!.CheckPointAfter);
        Assert.Equal(_settings.CheckPointLowerBound, result.Settings!.CheckPointLowerBound);
        Assert.Equal(_settings.CheckPointUpperBound, result.Settings!.CheckPointUpperBound);
        Assert.Equal(_settings.MaxSubscriberCount, result.Settings!.MaxSubscriberCount);
        Assert.Equal(_settings.ConsumerStrategyName, result.Settings!.ConsumerStrategyName);
    }

    [SupportsPSToAll.Fact]
    public async Task throws_with_non_existing_subscription() =>
        await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
            async () => {
                await _fixture.Client.GetInfoToAllAsync(
                    "NonExisting",
                    userCredentials: TestCredentials.Root
                );
            }
        );

    [SupportsPSToAll.Fact]
    public async Task throws_with_no_credentials() =>
        await Assert.ThrowsAsync<AccessDeniedException>(async () => { await _fixture.Client.GetInfoToAllAsync("NonExisting"); });

    [SupportsPSToAll.Fact]
    public async Task throws_with_non_existing_user() =>
        await Assert.ThrowsAsync<NotAuthenticatedException>(
            async () => {
                await _fixture.Client.GetInfoToAllAsync(
                    "NonExisting",
                    userCredentials: TestCredentials.TestBadUser
                );
            }
        );

    [SupportsPSToAll.Fact]
    public async Task returns_result_with_normal_user_credentials() {
        var result = await _fixture.Client.GetInfoToAllAsync(
            GroupName,
            userCredentials: TestCredentials.TestUser1
        );

        Assert.Equal("$all", result.EventSource);
    }

    void AssertKeyAndValue(IDictionary<string, long> items, string key) {
        Assert.True(items.ContainsKey(key));
        Assert.True(items[key] > 0);
    }

    public class Fixture : EventStoreClientFixture {
        public Fixture() : base(noDefaultCredentials: true) { }

        protected override async Task Given() {
            if (SupportsPSToAll.No)
                return;

            await Client.CreateToAllAsync(
                GroupName,
                _settings,
                userCredentials: TestCredentials.Root
            );
        }

        protected override async Task When() {
            if (SupportsPSToAll.No)
                return;

            var counter = 0;
            var tcs     = new TaskCompletionSource();

            await Client.SubscribeToAllAsync(
                GroupName,
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

            await tcs.Task;
        }
    }
}