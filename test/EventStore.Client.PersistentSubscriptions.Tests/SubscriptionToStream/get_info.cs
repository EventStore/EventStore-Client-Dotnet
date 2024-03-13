namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class get_info : IClassFixture<get_info.Fixture> {
	private const string GroupName = nameof(get_info);
	private const string StreamName = nameof(get_info);

	private static readonly PersistentSubscriptionSettings _settings = new(
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

	private readonly Fixture _fixture;

	public get_info(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object[]> AllowedUsers() {
		yield return new object[] { TestCredentials.Root };
		yield return new object[] { TestCredentials.TestUser1 };
	}

	[Theory]
	[MemberData(nameof(AllowedUsers))]
	public async Task returns_expected_result(UserCredentials credentials) {
		var result = await _fixture.Client.GetInfoToStreamAsync(StreamName, GroupName, userCredentials: credentials);

		Assert.Equal(StreamName, result.EventSource);
		Assert.Equal(GroupName, result.GroupName);
		Assert.NotNull(_settings.StartFrom);
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

	[Fact]
	public async Task throws_when_given_non_existing_subscription() =>
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () => {
				await _fixture.Client.GetInfoToStreamAsync(
					"NonExisting",
					"NonExisting",
					userCredentials: TestCredentials.Root
				);
			}
		);

	[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
	public async Task throws_with_non_existing_user() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(
			async () => {
				await _fixture.Client.GetInfoToStreamAsync(
					"NonExisting",
					"NonExisting",
					userCredentials: TestCredentials.TestBadUser
				);
			}
		);

	[Fact]
	public async Task throws_with_no_credentials() =>
		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				await _fixture.Client.GetInfoToStreamAsync(
					"NonExisting",
					"NonExisting"
				);
			}
		);

	[Fact]
	public async Task returns_result_for_normal_user() {
		var result = await _fixture.Client.GetInfoToStreamAsync(
			StreamName,
			GroupName,
			userCredentials: TestCredentials.TestUser1
		);

		Assert.NotNull(result);
	}

	private void AssertKeyAndValue(IDictionary<string, long> items, string key) {
		Assert.True(items.ContainsKey(key));
		Assert.True(items[key] > 0);
	}

	public class Fixture : EventStoreClientFixture {
		private EventStorePersistentSubscriptionsClient.PersistentSubscriptionResult? _subscription;
		private IAsyncEnumerator<PersistentSubscriptionMessage>? _enumerator;
		public Fixture() : base(noDefaultCredentials: true) { }

		protected override Task Given() =>
			Client.CreateToStreamAsync(groupName: GroupName, streamName: StreamName, settings: _settings,
				userCredentials: TestCredentials.Root);

		protected override async Task When() {
			var counter = 0;
			_subscription = Client.SubscribeToStream(StreamName, GroupName, userCredentials: TestCredentials.Root);
			_enumerator = _subscription.Messages.GetAsyncEnumerator();

			for (var i = 0; i < 15; i++) {
				await StreamsClient.AppendToStreamAsync(StreamName, StreamState.Any,
					new[] { new EventData(Uuid.NewUuid(), "test-event", ReadOnlyMemory<byte>.Empty) },
					userCredentials: TestCredentials.Root);
			}

			while (await _enumerator.MoveNextAsync()) {
				if (_enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
					continue;
				}

				counter++;

				if (counter == 1) {
					await _subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Test", resolvedEvent);
				}

				if (counter > 10) {
					return;
				}
			}
		}

		public override async Task DisposeAsync() {
			if (_enumerator is not null) {
				await _enumerator.DisposeAsync();
			}

			if (_subscription is not null) {
				await _subscription.DisposeAsync();
			}

			await base.DisposeAsync();
		}
	}
}
