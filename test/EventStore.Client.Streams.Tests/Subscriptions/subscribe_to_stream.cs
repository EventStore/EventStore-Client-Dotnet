namespace EventStore.Client.Streams.Tests.Subscriptions;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:Stream")]
public class subscribe_to_stream(ITestOutputHelper output, SubscriptionsFixture fixture) : EventStoreTests<SubscriptionsFixture>(output, fixture) {
	[Fact]
	public async Task receives_all_events_from_start() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));

		using var subscription = await Fixture.Streams
			.SubscribeToStreamAsync(streamName, FromStream.Start, OnReceived, false, OnDropped)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, seedEvents.Skip(pageSize));

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
	public async Task receives_all_events_from_non_existing_stream() {
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
	public async Task allow_multiple_subscriptions_to_same_stream() {
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
	public async Task drops_when_disposed() {
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
	public async Task drops_when_subscriber_error() {
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
	public async Task drops_when_stream_tombstoned() {
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
}