using System.Text;
using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllObsoleteTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_max_one_client() {
		// Arrange
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(maxSubscriberCount: 1), userCredentials: TestCredentials.Root);

		using var first = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			delegate { return Task.CompletedTask; },
			userCredentials: TestCredentials.Root
		).WithTimeout();

		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToAllAsync(
					group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root
				);
			}
		).WithTimeout();

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task connect_to_existing_with_permissions() {
		// Arrange
		var group = Fixture.GetGroupName();
		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			delegate { return Task.CompletedTask; },
			(s, reason, ex) => dropped.TrySetResult((reason, ex)),
			TestCredentials.Root
		).WithTimeout();

		Assert.NotNull(subscription);

		await Assert.ThrowsAsync<TimeoutException>(() => dropped.Task.WithTimeout());
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		// append 10 events to random streams to make sure we have at least 10 events in the transaction file
		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				Guid.NewGuid().ToString(),
				StreamState.NoStream,
				[@event]
			);
		}

		var events = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				firstEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstEventSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		var resolvedEvent = await firstEventSource.Task.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(events![0].Event.EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set_then_event_written() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		var expectedStreamId = Guid.NewGuid().ToString();
		var expectedEvent    = Fixture.CreateTestEvents(1).First();

		TaskCompletionSource<ResolvedEvent> firstNonSystemEventSource = new();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					await subscription.Ack(e);
					return;
				}

				firstNonSystemEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstNonSystemEventSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(expectedStreamId, StreamState.NoStream, [expectedEvent]);

		var resolvedEvent = await firstNonSystemEventSource.Task.WithTimeout();
		Assert.Equal(expectedEvent!.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_then_event_written() {
		var stream           = Fixture.GetStreamName();
		var group            = Fixture.GetGroupName();
		var expectedStreamId = Fixture.GetStreamName();
		var expectedEvent    = Fixture.CreateTestEvents().First();

		TaskCompletionSource<ResolvedEvent> firstNonSystemEventSource = new();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);
		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					await subscription.Ack(e);
					return;
				}

				firstNonSystemEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstNonSystemEventSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(expectedStreamId, StreamState.NoStream, [expectedEvent]);

		var resolvedEvent = await firstNonSystemEventSource.Task.WithTimeout();
		Assert.Equal(expectedEvent.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_invalid_middle_position() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> dropped = new();

		var invalidPosition = new Position(1L, 1L);
		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: invalidPosition),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => await subscription.Ack(e),
			(subscription, reason, ex) => { dropped.TrySetResult((reason, ex)); },
			TestCredentials.Root
		);

		var (reason, exception) = await dropped.Task.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_valid_middle_position() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		var expectedEvent = events[events.Length / 2]; //just a random event in the middle of the results

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: expectedEvent.OriginalPosition),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				firstEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstEventSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		var resolvedEvent = await firstEventSource.Task.WithTimeout();
		Assert.Equal(expectedEvent.OriginalPosition, resolvedEvent.Event.Position);
		Assert.Equal(expectedEvent.Event.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedEvent.Event.EventStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_with_retries() {
		// Arrange
		var group = Fixture.GetGroupName();

		TaskCompletionSource<int> retryCountSource = new();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

		// Act
		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, r, ct) => {
				if (r > 4) {
					retryCountSource.TrySetResult(r.Value);
					await subscription.Ack(e.Event.EventId);
				} else {
					await subscription.Nack(
						PersistentSubscriptionNakEventAction.Retry,
						"Not yet tried enough times",
						e
					);
				}
			},
			(subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					retryCountSource.TrySetException(ex!);
			},
			TestCredentials.Root
		);

		// Assert
		Assert.Equal(5, await retryCountSource.Task.WithTimeout());
	}

	[RetryFact]
	public async Task deleting_existing_with_subscriber() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> dropped = new();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, i, ct) => await s.Ack(e),
			(s, r, e) => dropped.TrySetResult((r, e)),
			TestCredentials.Root
		);

		// todo: investigate why this test is flaky without this delay
		await Task.Delay(500);

		await Fixture.Subscriptions.DeleteToAllAsync(group, userCredentials: TestCredentials.Root);

		var (reason, exception) = await dropped.Task.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	// [RetryFact]
	// public async Task happy_case_catching_up_to_link_to_events_manual_ack() {
	// 	var                        group              = Fixture.GetGroupName();
	// 	var                        bufferCount        = 10;
	// 	var                        eventWriteCount    = bufferCount * 2;
	// 	TaskCompletionSource<bool> eventsReceived     = new();
	// 	int                        eventReceivedCount = 0;
	//
	// 	var events = Fixture.CreateTestEvents(eventWriteCount)
	// 		.Select(
	// 			(e, i) => new EventData(
	// 				e.EventId,
	// 				SystemEventTypes.LinkTo,
	// 				Encoding.UTF8.GetBytes($"{i}@test"),
	// 				contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
	// 			)
	// 		)
	// 		.ToArray();
	//
	// 	foreach (var e in events) {
	// 		await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
	// 	}
	//
	// 	await Fixture.Subscriptions.CreateToAllAsync(
	// 		group,
	// 		new(startFrom: Position.Start, resolveLinkTos: true),
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
	// 		group,
	// 		async (subscription, e, retryCount, ct) => {
	// 			await subscription.Ack(e);
	//
	// 			if (e.OriginalStreamId.StartsWith("test-")
	// 			 && Interlocked.Increment(ref eventReceivedCount) == events.Length)
	// 				eventsReceived.TrySetResult(true);
	// 		},
	// 		(s, r, e) => {
	// 			if (e != null)
	// 				eventsReceived.TrySetException(e);
	// 		},
	// 		bufferSize: bufferCount,
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	await eventsReceived.Task.WithTimeout();
	// }
	//
	// [RetryFact]
	// public async Task happy_case_catching_up_to_normal_events_manual_ack() {
	// 	var group              = Fixture.GetGroupName();
	// 	var stream             = Fixture.GetStreamName();
	// 	var bufferCount        = 10;
	// 	var eventWriteCount    = bufferCount * 2;
	// 	int eventReceivedCount = 0;
	//
	// 	TaskCompletionSource<bool> eventsReceived = new();
	//
	// 	var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();
	//
	// 	foreach (var e in events)
	// 		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);
	//
	// 	await Fixture.Subscriptions.CreateToAllAsync(
	// 		group,
	// 		new(startFrom: Position.Start, resolveLinkTos: true),
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
	// 		group,
	// 		async (subscription, e, retryCount, ct) => {
	// 			await subscription.Ack(e);
	//
	// 			if (e.OriginalStreamId.StartsWith("test-")
	// 			 && Interlocked.Increment(ref eventReceivedCount) == events.Length)
	// 				eventsReceived.TrySetResult(true);
	// 		},
	// 		(s, r, e) => {
	// 			if (e != null)
	// 				eventsReceived.TrySetException(e);
	// 		},
	// 		bufferSize: bufferCount,
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	await eventsReceived.Task.WithTimeout();
	// }

	[RetryFact]
	public async Task happy_case_writing_and_subscribing_to_normal_events_manual_ack() {
		var group              = Fixture.GetGroupName();
		var bufferCount        = 10;
		var eventWriteCount    = bufferCount * 2;
		int eventReceivedCount = 0;

		TaskCompletionSource<bool> eventsReceived = new();

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: Position.End, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (subscription, e, retryCount, ct) => {
				await subscription.Ack(e);
				if (e.OriginalStreamId.StartsWith("test-")
				 && Interlocked.Increment(ref eventReceivedCount) == events.Length)
					eventsReceived.TrySetResult(true);
			},
			(s, r, e) => {
				if (e != null)
					eventsReceived.TrySetException(e);
			},
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
		}

		await eventsReceived.Task.WithTimeout();
	}

	[RetryFact]
	public async Task update_existing_with_check_point() {
		var group          = Fixture.GetGroupName();
		var events         = Fixture.CreateTestEvents(5).ToArray();
		var appearedEvents = new List<ResolvedEvent>();

		TaskCompletionSource<bool>                                    appeared      = new();
		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> droppedSource = new();
		TaskCompletionSource<ResolvedEvent>                           resumedSource = new();
		Position                                                      checkPoint    = default;

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, [e]);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: Position.Start),
			userCredentials: TestCredentials.Root
		);

		using var firstSubscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, r, ct) => {
				appearedEvents.Add(e);

				if (appearedEvents.Count == events.Length)
					appeared.TrySetResult(true);

				await s.Ack(e);
			},
			(subscription, reason, ex) => droppedSource.TrySetResult((reason, ex)),
			TestCredentials.Root
		);

		await Task.WhenAll(appeared.Task, WaitForCheckpoint().WithTimeout());

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await droppedSource.Task.WithTimeout();

		using var secondSubscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, r, ct) => {
				resumedSource.TrySetResult(e);
				await s.Ack(e);
				s.Dispose();
			},
			(_, reason, ex) => {
				if (ex is not null)
					resumedSource.TrySetException(ex);
				else
					resumedSource.TrySetResult(default);
			},
			TestCredentials.Root
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });

		var resumedEvent = await resumedSource.Task.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.True(resumedEvent.Event.Position > checkPoint);

		return;

		async Task WaitForCheckpoint() {
			await using var subscription = Fixture.Streams.SubscribeToStream(
				$"$persistentsubscription-$all::{group}-checkpoint",
				FromStream.Start,
				userCredentials: TestCredentials.Root
			);

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				checkPoint = resolvedEvent.Event.Data.ParsePosition();
				return;
			}
		}
	}

	[RetryFact]
	public async Task update_existing_with_check_point_filtered() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> droppedSource = new();
		TaskCompletionSource<ResolvedEvent>                           resumedSource = new();
		TaskCompletionSource<bool>                                    appeared      = new();

		List<ResolvedEvent> appearedEvents = [];

		EventData[] events = Fixture.CreateTestEvents(5).ToArray();

		Position checkPoint = default;

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, [e]);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			StreamFilter.Prefix("test"),
			new(
				checkPointLowerBound: 5,
				checkPointAfter: TimeSpan.FromSeconds(1),
				startFrom: Position.Start
			),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, r, ct) => {
				appearedEvents.Add(e);

				if (appearedEvents.Count == events.Length)
					appeared.TrySetResult(true);

				await s.Ack(e);
			},
			(subscription, reason, ex) => droppedSource.TrySetResult((reason, ex)),
			TestCredentials.Root
		);

		await Task.WhenAll(appeared.Task, Checkpointed()).WithTimeout();

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await droppedSource.Task.WithTimeout();

		await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, r, ct) => {
				resumedSource.TrySetResult(e);
				await s.Ack(e);
				s.Dispose();
			},
			(_, reason, ex) => {
				if (ex is not null)
					resumedSource.TrySetException(ex);
				else
					resumedSource.TrySetResult(default);
			},
			TestCredentials.Root
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });

		Task<ResolvedEvent> resumed = resumedSource.Task;

		var resumedEvent = await resumed.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.True(resumedEvent.Event.Position > checkPoint);

		return;

		async Task Checkpointed() {
			await using var subscription = Fixture.Streams.SubscribeToStream(
				$"$persistentsubscription-$all::{group}-checkpoint",
				FromStream.Start,
				userCredentials: TestCredentials.Root
			);

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				checkPoint = resolvedEvent.Event.Data.ParsePosition();
				return;
			}
		}
	}

	[RetryFact]
	public async Task update_existing_with_subscribers() {
		var group = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> droppedSource = new();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

		using var subscription = Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			delegate { return Task.CompletedTask; },
			(subscription, reason, ex) => droppedSource.TrySetResult((reason, ex)),
			TestCredentials.Root
		);

		// todo: investigate why this test is flaky without this delay
		await Task.Delay(500);

		await Fixture.Subscriptions.UpdateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var (reason, exception) = await droppedSource.Task.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task when_writing_and_filtering_out_events() {
		var events = Fixture.CreateTestEvents(10).ToArray();
		var group  = Fixture.GetGroupName();
		var prefix = Guid.NewGuid().ToString("N");

		TaskCompletionSource<bool> appeared = new();

		Position            firstCheckPoint  = default;
		Position            secondCheckPoint = default;
		List<ResolvedEvent> appearedEvents   = [];

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(prefix + Guid.NewGuid(), StreamState.Any, [e]);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			StreamFilter.Prefix(prefix),
			new(
				checkPointLowerBound: 1,
				checkPointUpperBound: 5,
				checkPointAfter: TimeSpan.FromSeconds(1),
				startFrom: Position.Start
			),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			async (s, e, r, ct) => {
				appearedEvents.Add(e);

				if (appearedEvents.Count == events.Length)
					appeared.TrySetResult(true);

				await s.Ack(e);
			},
			userCredentials: TestCredentials.Root
		);

		await Task.WhenAll(appeared.Task, WaitForCheckpoints().WithTimeout());

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync(
				"filtered-out-stream-" + Guid.NewGuid(),
				StreamState.Any,
				[e]
			);
		}

		Assert.True(secondCheckPoint > firstCheckPoint);
		Assert.Equal(events.Select(e => e.EventId), appearedEvents.Select(e => e.Event.EventId));

		return;

		async Task WaitForCheckpoints() {
			bool firstCheckpointSet = false;
			await using var subscription = Fixture.Streams.SubscribeToStream(
				$"$persistentsubscription-$all::{group}-checkpoint",
				FromStream.Start,
				userCredentials: TestCredentials.Root
			);

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!firstCheckpointSet) {
					firstCheckPoint    = resolvedEvent.Event.Data.ParsePosition();
					firstCheckpointSet = true;
				} else {
					secondCheckPoint = resolvedEvent.Event.Data.ParsePosition();
					return;
				}
			}
		}
	}

	// [RetryFact]
	// public async Task when_writing_and_subscribing_to_normal_events_manual_nack() {
	// 	var group           = Fixture.GetGroupName();
	// 	var bufferCount     = 10;
	// 	var eventWriteCount = bufferCount * 2;
	// 	var prefix          = $"{Guid.NewGuid():N}-";
	//
	// 	var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();
	//
	// 	await Fixture.Subscriptions.CreateToAllAsync(
	// 		group,
	// 		new(startFrom: Position.Start, resolveLinkTos: true),
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	var subscription = Fixture.Subscriptions.SubscribeToAll(group, bufferSize: bufferCount, userCredentials: TestCredentials.Root);
	//
	// 	foreach (var e in events)
	// 		await Fixture.Streams.AppendToStreamAsync(prefix + Guid.NewGuid(), StreamState.Any, [e]);
	//
	// 	await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
	// 		.SelectAwait(
	// 			async e => {
	// 				await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "fail", e.ResolvedEvent);
	// 				return e;
	// 			}
	// 		)
	// 		.Where(e => e.ResolvedEvent.OriginalStreamId.StartsWith(prefix))
	// 		.Take(events.Length)
	// 		.ToArrayAsync()
	// 		.AsTask()
	// 		.WithTimeout();
	// }

	// [RetryFact]
	// public async Task update_existing_with_commit_position_larger_than_last_indexed_position() {
	// 	var group = Fixture.GetGroupName();
	//
	// 	await Fixture.Subscriptions.CreateToAllAsync(
	// 		group,
	// 		new(),
	// 		userCredentials: TestCredentials.Root
	// 	);
	//
	// 	var lastEvent = await Fixture.Streams.ReadAllAsync(
	// 		Direction.Backwards,
	// 		Position.End,
	// 		1,
	// 		userCredentials: TestCredentials.Root
	// 	).FirstAsync();
	//
	// 	var lastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
	// 	var ex = await Assert.ThrowsAsync<RpcException>(
	// 		() =>
	// 			Fixture.Subscriptions.UpdateToAllAsync(
	// 				group,
	// 				new(startFrom: new Position(lastCommitPosition + 1, lastCommitPosition)),
	// 				userCredentials: TestCredentials.Root
	// 			)
	// 	);
	//
	// 	Assert.Equal(StatusCode.Internal, ex.StatusCode);
	// }
}
