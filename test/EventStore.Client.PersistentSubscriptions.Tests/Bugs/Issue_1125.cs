namespace EventStore.Client.PersistentSubscriptions.Tests.Bugs;

public class Issue_1125 : IClassFixture<Issue_1125.Fixture> {
	readonly Fixture _fixture;

	public Issue_1125(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object?[]> TestCases() => Enumerable.Range(0, 50).Select(i => new object[] { i });

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task persistent_subscription_delivers_all_events(int iteration) {
		const int eventCount = 250;
		const int totalEvents = eventCount * 2;

		var hitCount = 0;

		var userCredentials = new UserCredentials("admin", "changeit");

		var streamName = $"stream_{iteration}";
		var subscriptionName = $"subscription_{iteration}";

		for (var i = 0; i < eventCount; i++)
			await _fixture.StreamsClient.AppendToStreamAsync(
				streamName,
				StreamState.Any,
				_fixture.CreateTestEvents()
			);

		await _fixture.Client.CreateToStreamAsync(
			streamName,
			subscriptionName,
			new(
				true,
				StreamPosition.Start,
				readBatchSize: 10,
				historyBufferSize: 20
			),
			userCredentials: userCredentials
		);

		await using var subscription =
			_fixture.Client.SubscribeToStream(streamName, subscriptionName, userCredentials: userCredentials);

		await Task.WhenAll(Subscribe(), Append()).WithTimeout();

		Assert.Equal(totalEvents, hitCount);

		return;

		async Task Subscribe() {
			await foreach (var message in subscription.Messages) {
				if (message is not PersistentSubscriptionMessage.Event(var resolvedEvent, var retryCount)) {
					continue;
				}

				if (retryCount is 0 or null) {
					var result = Interlocked.Increment(ref hitCount);

					await subscription.Ack(resolvedEvent);

					if (totalEvents == result)
						return;
				} else {
					// This is a retry
					await subscription.Ack(resolvedEvent);
				}
			}
		}

		async Task Append() {
			for (var i = 0; i < eventCount; i++)
				await _fixture.StreamsClient.AppendToStreamAsync(
					streamName,
					StreamState.Any,
					_fixture.CreateTestEvents());
		}
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When() => Task.CompletedTask;
	}
}
