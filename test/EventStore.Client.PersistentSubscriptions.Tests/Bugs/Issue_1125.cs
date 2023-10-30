namespace EventStore.Client.PersistentSubscriptions.Tests.Bugs;

public class Issue_1125 : IClassFixture<Issue_1125.Fixture> {
	readonly Fixture _fixture;

	public Issue_1125(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object?[]> TestCases() => Enumerable.Range(0, 50).Select(i => new object[] { i });

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task persistent_subscription_delivers_all_events(int iteration) {
		if (Environment.OSVersion.IsWindows()) { }

		const int eventCount  = 250;
		const int totalEvents = eventCount * 2;

		var completed = new TaskCompletionSource<bool>();
		var hitCount  = 0;

		var userCredentials = new UserCredentials("admin", "changeit");

		var streamName       = $"stream_{iteration}";
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

		using (await _fixture.Client.SubscribeToStreamAsync(
			       streamName,
			       subscriptionName,
			       async (subscription, @event, retryCount, arg4) => {
				       int result;
				       if (retryCount == 0 || retryCount is null) {
					       result = Interlocked.Increment(ref hitCount);

					       await subscription.Ack(@event);

					       if (totalEvents == result)
						       completed.TrySetResult(true);
				       }
				       else {
					       // This is a retry
					       await subscription.Ack(@event);
				       }
			       },
			       (s, dr, e) => {
				       if (e != null)
					       completed.TrySetException(e);
				       else
					       completed.TrySetException(new Exception($"{dr}"));
			       },
			       userCredentials
		       )) {
			for (var i = 0; i < eventCount; i++)
				await _fixture.StreamsClient.AppendToStreamAsync(
					streamName,
					StreamState.Any,
					_fixture.CreateTestEvents()
				);

			await completed.Task.WithTimeout(TimeSpan.FromSeconds(30));
		}

		Assert.Equal(totalEvents, hitCount);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}