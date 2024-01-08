namespace EventStore.Client.Streams.Tests.Bugs;

[Trait("Category", "Bug")]
public class Issue_104(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	[Fact]
	public async Task Callback_subscription_does_not_send_checkpoint_reached_after_disposal() {
		var streamName                   = Fixture.GetStreamName();
		var ignoredStreamName            = $"ignore_{streamName}";
		var subscriptionDisposed         = new TaskCompletionSource<bool>();
		var eventAppeared                = new TaskCompletionSource<bool>();
		var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamRevision.None, Fixture.CreateTestEvents());

		var subscription = await Fixture.Streams.SubscribeToAllAsync(
			FromAll.Start,
			(_, _, _) => {
				eventAppeared.TrySetResult(true);
				return Task.CompletedTask;
			},
			false,
			(_, _, _) => subscriptionDisposed.TrySetResult(true),
			caughtUp: null,
			fellBehind: null,
			new(
				StreamFilter.Prefix(streamName),
				1,
				(_, _, _) => {
					if (!subscriptionDisposed.Task.IsCompleted)
						return Task.CompletedTask;

					checkpointReachAfterDisposed.TrySetResult(true);
					return Task.CompletedTask;
				}
			),
			new("admin", "changeit")
		);

		await eventAppeared.Task;

		subscription.Dispose();
		await subscriptionDisposed.Task;

		await Fixture.Streams.AppendToStreamAsync(
			ignoredStreamName,
			StreamRevision.None,
			Fixture.CreateTestEvents(50)
		);

		var delay  = Task.Delay(300);
		var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
		result.ShouldBe(delay); // iow 300ms have passed without seeing checkpointReachAfterDisposed
	}


	[Fact]
	public async Task Iterator_API_subscription_does_not_send_checkpoint_reached_after_disposal() {
		var streamName                   = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";
		var ignoredStreamName            = $"ignore_{streamName}_{Guid.NewGuid()}";
		var subscriptionDisposed         = new TaskCompletionSource<bool>();
		var eventAppeared                = new TaskCompletionSource<bool>();
		var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamRevision.None, Fixture.CreateTestEvents());

		var subscription = Fixture.Streams.SubscribeToAll(FromAll.Start,false, new SubscriptionFilterOptions(StreamFilter.Prefix(streamName)));

		ReadMessages(subscription, _ => {
			eventAppeared.TrySetResult(true);
			return Task.CompletedTask;
		}, _ => subscriptionDisposed.TrySetResult(true), _ => {
			if (!subscriptionDisposed.Task.IsCompleted) {
				return Task.CompletedTask;
			}

			checkpointReachAfterDisposed.TrySetResult(true);
			return Task.CompletedTask;
		});

		await eventAppeared.Task;

		subscription.Dispose();
		await subscriptionDisposed.Task;

		await Fixture.Streams.AppendToStreamAsync(ignoredStreamName, StreamRevision.None,
			Fixture.CreateTestEvents(50));

		var delay  = Task.Delay(300);
		var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
		Assert.Equal(delay, result); // iow 300ms have passed without seeing checkpointReachAfterDisposed
	}

	async void ReadMessages(EventStoreClient.SubscriptionResult subscription, Func<ResolvedEvent, Task> eventAppeared, Action<Exception?> subscriptionDropped, Func<Position, Task> checkpointReached) {
		Exception? exception = null;
		try {
			await foreach (var message in subscription.Messages) {
				if (message is StreamMessage.Event eventMessage) {
					await eventAppeared(eventMessage.ResolvedEvent);
				} else if (message is StreamMessage.SubscriptionMessage.Checkpoint checkpointMessage) {
					await checkpointReached(checkpointMessage.Position);
				}
			}
		} catch (Exception ex) {
			exception = ex;
		}

		//allow some time for subscription cleanup and chance for exception to be raised
		await Task.Delay(100);

		try {
			//subscription.SubscriptionState will throw exception if some problem occurred for the subscription
			Assert.Equal(SubscriptionState.Disposed, subscription.SubscriptionState);
			subscriptionDropped(exception);
		} catch (Exception ex) {
			subscriptionDropped(ex);
		}
	}
}
