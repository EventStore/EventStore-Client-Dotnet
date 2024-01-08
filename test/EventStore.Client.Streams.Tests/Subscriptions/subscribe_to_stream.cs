namespace EventStore.Client.Streams.Tests.Subscriptions;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:Stream")]
public class subscribe_to_stream(ITestOutputHelper output, SubscriptionsFixture fixture) : EventStoreTests<SubscriptionsFixture>(output, fixture) {
	[Fact]
	public async Task Callback_receives_all_events_from_start() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		var caughtUpCalled      = new TaskCompletionSource<bool>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, FromStream.Start, OnReceived, false, OnDropped, OnCaughtUp)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, seedEvents.Skip(pageSize));

		await receivedAllEvents.Task.WithTimeout();
		if (Fixture.EventStoreHasCaughtUpAndFellBehind) {
			await caughtUpCalled.Task.WithTimeout();
		} else {
			caughtUpCalled.TrySetResult(true);
		}

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());

		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId);

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}

			return Task.CompletedTask;
		}

		Task OnCaughtUp(StreamSubscription sub, CancellationToken ct) {
			Fixture.Log.Information("Subscription has caught up");
			caughtUpCalled.TrySetResult(true);
			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) =>
			subscriptionDropped.SetResult(new(reason, ex));
	}

	[Fact]
	public async Task Iterator_receives_all_events_from_start() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<Exception?>();
		var caughtUpCalled      = new TaskCompletionSource<bool>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));

		var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start, false);
		ReadMessages(subscription, EventAppeared, SubscriptionDropped, OnCaughtUp);

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, seedEvents.Skip(pageSize));

		await receivedAllEvents.Task.WithTimeout();
		if (Fixture.EventStoreHasCaughtUpAndFellBehind) {
			await caughtUpCalled.Task.WithTimeout();
		} else {
			caughtUpCalled.TrySetResult(true);
		}

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result?.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(null);

		return;

		Task EventAppeared(ResolvedEvent re) {
			availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId);

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}

			return Task.CompletedTask;
		}

		Task OnCaughtUp(EventStoreClient.SubscriptionResult sub) {
			Fixture.Log.Information("Subscription has caught up");
			caughtUpCalled.TrySetResult(true);
			return Task.CompletedTask;
		}

		void SubscriptionDropped(Exception? ex) => subscriptionDropped.SetResult(ex);
	}

	[Fact]
	public async Task receives_all_events_from_position() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		var writeResult    = await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));
		var streamPosition = StreamPosition.FromStreamRevision(writeResult.NextExpectedStreamRevision);
		var checkpoint     = FromStream.After(streamPosition);

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, checkpoint, OnReceived, false, OnDropped)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(streamName, writeResult.NextExpectedStreamRevision, seedEvents.Skip(pageSize));

		await receivedAllEvents.Task.WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());

		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId);

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", pageSize);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) =>
			subscriptionDropped.SetResult(new(reason, ex));
	}

	[Fact]
	public async Task Callback_receives_all_events_from_non_existing_stream() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, FromStream.Start, OnReceived, false, OnDropped)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await receivedAllEvents.Task.WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());

		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId);

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) =>
			subscriptionDropped.SetResult(new(reason, ex));
	}

	[Fact]
	public async Task Iterator_subscribe_to_non_existing_stream() {
		var stream   = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";
		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<Exception?>();

		var subscription = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		ReadMessages(subscription, EventAppeared, SubscriptionDropped);

		Assert.False(appeared.Task.IsCompleted);

		if (dropped.Task.IsCompleted) {
			Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
		}

		subscription.Dispose();

		var ex = await dropped.Task.WithTimeout();
		Assert.Null(ex);

		Task EventAppeared(ResolvedEvent e) {
			appeared.TrySetResult(true);
			return Task.CompletedTask;
		}

		void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
	}

	[Fact]
	public async Task Callback_allow_multiple_subscriptions_to_same_stream() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents = new TaskCompletionSource<bool>();

		var seedEvents = Fixture.CreateTestEvents(5).ToArray();

		var targetEventsCount = seedEvents.Length * 2;

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		using var subscription1 = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, FromStream.Start, OnReceived)
			.WithTimeout();

		using var subscription2 = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, FromStream.Start, OnReceived)
			.WithTimeout();

		await receivedAllEvents.Task.WithTimeout();

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			if (--targetEventsCount == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}

			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task Iterator_allow_multiple_subscriptions_to_same_stream() {
		var stream = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";

		var appeared = new TaskCompletionSource<bool>();

		int appearedCount = 0;

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		var s1 = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		ReadMessages(s1, EventAppeared, null);

		var s2 = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		ReadMessages(s2, EventAppeared, null);

		Assert.True(await appeared.Task.WithTimeout());

		Task EventAppeared(ResolvedEvent e) {
			if (++appearedCount == 2) {
				appeared.TrySetResult(true);
			}

			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task Callback_drops_when_disposed() {
		var streamName = Fixture.GetStreamName();

		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				streamName,
				FromStream.Start,
				(sub, re, ct) => Task.CompletedTask,
				false,
				(sub, reason, ex) => subscriptionDropped.SetResult(new(reason, ex))
			)
			.WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());
	}

	[Fact]
	public async Task Iterator_client_stops_reading_messages_when_subscription_disposed() {
		var stream  = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";
		var dropped = new TaskCompletionSource<Exception?>();

		var subscription = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		var testEvent    = Fixture.CreateTestEvents(1).First();
		ReadMessages(subscription, EventAppeared, SubscriptionDropped);

		if (dropped.Task.IsCompleted) {
			Assert.False(dropped.Task.IsCompleted, dropped.Task.Result?.ToString());
		}

		subscription.Dispose();

		var ex = await dropped.Task.WithTimeout();
		Assert.Null(ex);

		// new event after subscription is disposed
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, new[]{testEvent});

		Task EventAppeared(ResolvedEvent e) {
			return testEvent.EventId.Equals(e.OriginalEvent.EventId) ? Task.FromException(new Exception("Subscription not dropped")) : Task.CompletedTask;
		}

		void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
	}

	[Fact]
	public async Task Callback_drops_when_subscriber_error() {
		var streamName = Fixture.GetStreamName();

		var expectedResult = SubscriptionDroppedResult.SubscriberError();

		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				streamName,
				FromStream.Start,
				(sub, re, ct) => expectedResult.Throw(),
				false,
				(sub, reason, ex) => subscriptionDropped.SetResult(new(reason, ex))
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, Fixture.CreateTestEvents());

		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task Iterator_client_stops_reading_messages_when_error_processing_event() {
		var stream            = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";
		var dropped           = new TaskCompletionSource<Exception?>();
		var expectedException = new Exception("Error");
		int numTimesCalled    = 0;

		var subscription = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		ReadMessages(subscription, EventAppeared, SubscriptionDropped);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		var ex = await dropped.Task.WithTimeout();
		Assert.Same(expectedException, ex);

		Assert.Equal(1, numTimesCalled);

		Task EventAppeared(ResolvedEvent e) {
			numTimesCalled++;
			return Task.FromException(expectedException);
		}

		void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
	}

	[Fact]
	public async Task Callback_drops_when_stream_tombstoned() {
		var streamName = Fixture.GetStreamName();

		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(
				streamName,
				FromStream.Start,
				(sub, re, ct) => Task.CompletedTask,
				false,
				(sub, reason, ex) => { subscriptionDropped.SetResult(new(reason, ex)); }
			)
			.WithTimeout();

		// rest in peace
		await Fixture.Streams.TombstoneAsync(streamName, StreamState.NoStream);

		var result = await subscriptionDropped.Task.WithTimeout();
		result.Error.ShouldBeOfType<StreamDeletedException>().Stream.ShouldBe(streamName);
	}

	[Fact]
	public async Task Iterator_drops_when_stream_tombstoned() {
		var stream = $"{Fixture.GetStreamName()}_{Guid.NewGuid()}";

		var dropped = new TaskCompletionSource<Exception?>();

		var subscription = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
		ReadMessages(subscription, EventAppeared, SubscriptionDropped);

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);
		var ex = await dropped.Task.WithTimeout();

		var sdex = Assert.IsType<StreamDeletedException>(ex);
		Assert.Equal(stream, sdex.Stream);

		Task EventAppeared(ResolvedEvent e) => Task.CompletedTask;

		void SubscriptionDropped(Exception? ex) => dropped.SetResult(ex);
	}

	[Fact]
	public async Task receives_all_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync($"$et-{EventStoreFixture.TestEventType}", FromStream.Start, OnReceived, true, OnDropped)
			.WithTimeout();

		await receivedAllEvents.Task.WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());

		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());

		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			var hasResolvedLink = re.OriginalEvent.EventStreamId.StartsWith($"$et-{EventStoreFixture.TestEventType}");
			if (availableEvents.RemoveWhere(x => x == re.Event.EventId && hasResolvedLink) == 0) {
				Fixture.Log.Debug("Received unexpected event {EventId} from stream {StreamId}", re.Event.EventId, re.OriginalEvent.EventStreamId);
				return Task.CompletedTask;
			}

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", seedEvents.Length);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) =>
			subscriptionDropped.SetResult(new(reason, ex));
	}

	async void ReadMessages(
		EventStoreClient.SubscriptionResult subscription,
		Func<ResolvedEvent, Task> eventAppeared,
		Action<Exception?>? subscriptionDropped,
		Func<EventStoreClient.SubscriptionResult, Task>? caughtUp = null
	) {
		Exception? exception = null;
		try {
			await foreach (var message in subscription.Messages) {
				switch (message) {
					case StreamMessage.Event eventMessage: await eventAppeared(eventMessage.ResolvedEvent);
						break;

					case StreamMessage.SubscriptionMessage.CaughtUp: {
						if (caughtUp is not null) await caughtUp(subscription);
						break;
					}
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
			subscriptionDropped?.Invoke(exception);
		} catch (Exception ex) {
			subscriptionDropped?.Invoke(ex);
		}
	}
}
