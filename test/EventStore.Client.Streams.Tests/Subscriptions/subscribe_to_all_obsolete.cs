namespace EventStore.Client.Streams.Tests.Subscriptions;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:All")]
[Obsolete]
public class subscribe_to_all_obsolete(ITestOutputHelper output, SubscriptionsFixture fixture) : EventStoreTests<SubscriptionsFixture>(output, fixture) {
	[Fact]
	public async Task receives_all_events_from_start() {
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;
		
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));
		
		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"stream-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.Start, OnReceived, false, OnDropped)
			.WithTimeout();

		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"stream-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

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
	public async Task receives_all_events_from_end() {
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.End, OnReceived, false, OnDropped)
			.WithTimeout();

		// add the events we want to receive after we start the subscription
		foreach (var evt in seedEvents)
			await Fixture.Streams.AppendToStreamAsync($"stream-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

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
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;
		
		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));
		
		IWriteResult writeResult = new SuccessResult();
		foreach (var evt in seedEvents.Take(pageSize))
			writeResult = await Fixture.Streams.AppendToStreamAsync($"stream-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

		var position = FromAll.After(writeResult.LogPosition);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(position, OnReceived, false, OnDropped)
			.WithTimeout();

		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"stream-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

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
	public async Task receives_all_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));
		
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.Start, OnReceived, true, OnDropped)
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
	
	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType= typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_start(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_start)}-{filter.Name}-{Guid.NewGuid():N}";
		
		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);
		
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		var checkpointReached   = new TaskCompletionSource<bool>();
		
		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();

		var pageSize = seedEvents.Length / 2;
		
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(Fixture.GetStreamName(), StreamState.NoStream, Fixture.CreateTestEvents(3));
		
		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start).CountAsync();
		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);

		// Debugging:
		// await foreach (var evt in Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start))
		// 	Fixture.Log.Debug("Read event {EventId} from {StreamId}.", evt.OriginalEvent.EventId, evt.OriginalEvent.EventStreamId);
		
		// add some of the events we want to see before we start the subscription
		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1, CheckpointReached);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.Start, OnReceived, false, OnDropped, filterOptions)
			.WithTimeout();

		// add some of the events we want to see after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });
		
		// wait until all events were received and at least one checkpoint was reached?
		await receivedAllEvents.Task.WithTimeout();
		await checkpointReached.Task.WithTimeout();
		
		// await Task.WhenAll(receivedAllEvents.Task, checkpointReached.Task).WithTimeout();

		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());
		
		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			if (availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId) == 0) {
				Fixture.Log.Error(
					"Received unexpected event {EventId} from {StreamId}",
					re.OriginalEvent.EventId,
					re.OriginalEvent.EventStreamId
				);

				receivedAllEvents.TrySetException(
					new InvalidOperationException($"Received unexpected event {re.OriginalEvent.EventId} from stream {re.OriginalEvent.EventStreamId}")
				);
			}
			else {
				Fixture.Log.Verbose("Received expected event {EventId} from {StreamId}.", re.OriginalEvent.EventId, re.OriginalEvent.EventStreamId);
			}

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events.", seedEvents.Length);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) {
			subscriptionDropped.SetResult(new(reason, ex));
			if (reason != SubscriptionDroppedReason.Disposed) {
				receivedAllEvents.TrySetException(ex!);
				checkpointReached.TrySetException(ex!);
			}
		}

		Task CheckpointReached(StreamSubscription sub, Position position, CancellationToken ct) {
			Fixture.Log.Verbose(
				"Checkpoint reached {Position}. Received {ReceivedEventsCount}/{TotalEventsCount} events",
				position, seedEvents.Length - availableEvents.Count, seedEvents.Length
			);
			checkpointReached.TrySetResult(true);
			return Task.CompletedTask;
		}
	}
	
	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType= typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_end(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_end)}-{filter.Name}-{Guid.NewGuid():N}";
		
		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);
		
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		var checkpointReached   = new TaskCompletionSource<bool>();
		
		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();
		
		var pageSize = seedEvents.Length / 2;
		
		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(Fixture.GetStreamName(), StreamState.NoStream, Fixture.CreateTestEvents(3));
		
		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start).CountAsync();
		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);
		
		// add some of the events that are a match to the filter but will not be received
		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });
		
		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1, CheckpointReached);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.End, OnReceived, false, OnDropped, filterOptions)
			.WithTimeout();

		// add the events we want to receive after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });
		
		// wait until all events were received and at least one checkpoint was reached?
		await receivedAllEvents.Task.WithTimeout();
		await checkpointReached.Task.WithTimeout();
		
		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());
		
		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());
		
		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			if (availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId) == 0) {
				Fixture.Log.Error(
					"Received unexpected event {EventId} from {StreamId}",
					re.OriginalEvent.EventId,
					re.OriginalEvent.EventStreamId
				);

				receivedAllEvents.TrySetException(
					new InvalidOperationException($"Received unexpected event {re.OriginalEvent.EventId} from stream {re.OriginalEvent.EventStreamId}")
				);
			}
			else {
				Fixture.Log.Verbose("Received expected event {EventId} from {StreamId}", re.OriginalEvent.EventId, re.OriginalEvent.EventStreamId);
			}

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", pageSize);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) {
			subscriptionDropped.SetResult(new(reason, ex));
			if (reason != SubscriptionDroppedReason.Disposed) {
				receivedAllEvents.TrySetException(ex!);
				checkpointReached.TrySetException(ex!);
			}
		}

		Task CheckpointReached(StreamSubscription sub, Position position, CancellationToken ct) {
			Fixture.Log.Verbose(
				"Checkpoint reached {Position}. Received {ReceivedEventsCount}/{TotalEventsCount} events",
				position, pageSize - availableEvents.Count, pageSize
			);
			checkpointReached.TrySetResult(true);
			return Task.CompletedTask;
		}
	}

	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType= typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_position(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_position)}-{filter.Name}-{Guid.NewGuid():N}";
		
		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);
		
		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		var checkpointReached   = new TaskCompletionSource<bool>();
		
		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();
		
		var pageSize = seedEvents.Length / 2;
		
		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(Fixture.GetStreamName(), StreamState.NoStream, Fixture.CreateTestEvents(3));
		
		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start).CountAsync();
		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);
		
		// add some of the events that are a match to the filter but will not be received
		IWriteResult writeResult = new SuccessResult();
		foreach (var evt in seedEvents.Take(pageSize))
			writeResult = await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });

		var position = FromAll.After(writeResult.LogPosition);
		
		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1, CheckpointReached);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(position, OnReceived, false, OnDropped, filterOptions)
			.WithTimeout();

		// add the events we want to receive after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync($"{streamPrefix}-{evt.EventId.ToGuid():N}", StreamState.NoStream, new[] { evt });
		
		// wait until all events were received and at least one checkpoint was reached?
		await receivedAllEvents.Task.WithTimeout();
		await checkpointReached.Task.WithTimeout();
		
		// if the subscription dropped before time, raise the reason why
		if (subscriptionDropped.Task.IsCompleted)
			subscriptionDropped.Task.IsCompleted.ShouldBe(false, subscriptionDropped.Task.Result.ToString());
		
		// stop the subscription
		subscription.Dispose();
		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(SubscriptionDroppedResult.Disposed());
		
		return;

		Task OnReceived(StreamSubscription sub, ResolvedEvent re, CancellationToken ct) {
			if (availableEvents.RemoveWhere(x => x == re.OriginalEvent.EventId) == 0) {
				Fixture.Log.Error(
					"Received unexpected event {EventId} from {StreamId}",
					re.OriginalEvent.EventId,
					re.OriginalEvent.EventStreamId
				);

				receivedAllEvents.TrySetException(
					new InvalidOperationException($"Received unexpected event {re.OriginalEvent.EventId} from stream {re.OriginalEvent.EventStreamId}")
				);
			}
			else {
				Fixture.Log.Verbose("Received expected event {EventId} from {StreamId}", re.OriginalEvent.EventId, re.OriginalEvent.EventStreamId);
			}

			if (availableEvents.Count == 0) {
				receivedAllEvents.TrySetResult(true);
				Fixture.Log.Information("Received all {TotalEventsCount} expected events", pageSize);
			}

			return Task.CompletedTask;
		}

		void OnDropped(StreamSubscription sub, SubscriptionDroppedReason reason, Exception? ex) {
			subscriptionDropped.SetResult(new(reason, ex));
			if (reason != SubscriptionDroppedReason.Disposed) {
				receivedAllEvents.TrySetException(ex!);
				checkpointReached.TrySetException(ex!);
			}
		}

		Task CheckpointReached(StreamSubscription sub, Position position, CancellationToken ct) {
			Fixture.Log.Verbose(
				"Checkpoint reached {Position}. Received {ReceivedEventsCount}/{TotalEventsCount} events",
				position, pageSize - availableEvents.Count, pageSize
			);
			checkpointReached.TrySetResult(true);
			return Task.CompletedTask;
		}
	}
	
	[Fact]
	public async Task receives_all_filtered_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var receivedAllEvents   = new TaskCompletionSource<bool>();
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));
		
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		var options = new SubscriptionFilterOptions(
			StreamFilter.Prefix($"$et-{EventStoreFixture.TestEventType}")
		);
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(FromAll.Start, OnReceived, true, OnDropped, options)
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
	
	[Fact]
	public async Task drops_when_disposed() {
		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(
				FromAll.Start,
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
		var expectedResult = SubscriptionDroppedResult.SubscriberError();

		var subscriptionDropped = new TaskCompletionSource<SubscriptionDroppedResult>();
		
		using var subscription = await Fixture.Streams
			.SubscribeToAllAsync(
				FromAll.Start,
				(sub, re, ct) => expectedResult.Throw(),
				false,
				(sub, reason, ex) => subscriptionDropped.SetResult(new(reason, ex))
			)
			.WithTimeout();

		await Fixture.Streams.AppendToStreamAsync(Fixture.GetStreamName(), StreamState.NoStream, Fixture.CreateTestEvents());

		var result = await subscriptionDropped.Task.WithTimeout();
		result.ShouldBe(expectedResult);
	}
}
