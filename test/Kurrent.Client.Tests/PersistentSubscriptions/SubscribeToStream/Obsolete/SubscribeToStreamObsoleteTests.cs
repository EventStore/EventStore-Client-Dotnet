using System.Text;
using EventStore.Client;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamObsoleteTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_max_one_client() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(maxSubscriberCount: 1),
			userCredentials: TestCredentials.Root
		);

		using var first = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			delegate { return Task.CompletedTask; },
			userCredentials: TestCredentials.Root
		).WithTimeout();

		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToStreamAsync(
					stream,
					group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root
				);
			}
		).WithTimeout();

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task connect_to_existing_with_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			delegate { return Task.CompletedTask; },
			(s, reason, ex) => dropped.TrySetResult((reason, ex)),
			TestCredentials.Root
		).WithTimeout();

		Assert.NotNull(subscription);

		await Assert.ThrowsAsync<TimeoutException>(() => dropped.Task.WithTimeout());
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning_and_events_in_it() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(StreamPosition.Start, resolvedEvent.Event.EventNumber);
		Assert.Equal(events[0].EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning_and_no_stream() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents().ToArray();

		var eventId = events.Single().EventId;

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var firstEvent = await firstEventSource.Task.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(StreamPosition.Start, firstEvent.Event.EventNumber);
		Assert.Equal(eventId, firstEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set_and_events_in_it() {
		var                                 stream           = Fixture.GetStreamName();
		var                                 group            = Fixture.GetGroupName();
		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		var firstEvent = firstEventSource.Task;

		await Assert.ThrowsAsync<TimeoutException>(() => firstEvent.WithTimeout());
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(11).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_and_events_in_it() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		var firstEvent = firstEventSource.Task;
		await Assert.ThrowsAsync<TimeoutException>(() => firstEvent.WithTimeout());
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(11).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_two_and_no_stream() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(2)),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var firstEvent    = firstEventSource.Task;
		var resolvedEvent = await firstEvent.WithTimeout(TimeSpan.FromSeconds(10));
		var eventId       = events.Last().EventId;

		Assert.Equal(new(2), resolvedEvent.Event.EventNumber);
		Assert.Equal(eventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_and_events_in_it() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(4)),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(4), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Skip(4).First().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(11).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(10)),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(12).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(11));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(11)),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(10), events.Skip(11));

		var firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(11), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_non_existing_with_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		var ex = await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToStreamAsync(
					stream,
					group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root
				);
			}
		).WithTimeout();

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task connect_with_retries() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<int> retryCountSource = new();

		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		Task<int> retryCount = retryCountSource.Task;
		Assert.Equal(5, await retryCount.WithTimeout());
	}

	[RetryFact]
	public async Task connecting_to_a_persistent_subscription() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<ResolvedEvent> firstEventSource = new();

		var events = Fixture.CreateTestEvents(12).ToArray();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(11));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(11)),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
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

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(10), events.Skip(11));

		Task<ResolvedEvent> firstEvent = firstEventSource.Task;

		var resolvedEvent = await firstEvent.WithTimeout();
		Assert.Equal(new(11), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task deleting_existing_with_subscriber_the_subscription_is_dropped() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> dropped = new();
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			(_, _, _, _) => Task.CompletedTask,
			(_, r, e) => dropped.TrySetResult((r, e)),
			TestCredentials.Root
		);

		await Fixture.Subscriptions.DeleteToStreamAsync(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		var (reason, exception) = await dropped.Task.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[Fact(Skip = "Isn't this how it should work?")]
	public async Task deleting_existing_with_subscriber_the_subscription_is_dropped_with_not_found() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _dropped = new();
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			(_, _, _, _) => Task.CompletedTask,
			(_, r, e) => _dropped.TrySetResult((r, e)),
			TestCredentials.Root
		);

		await Fixture.Subscriptions.DeleteToStreamAsync(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		Task<(SubscriptionDroppedReason, Exception?)> dropped = _dropped.Task;

		var (reason, exception) = await dropped.WithTimeout();
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionNotFoundException>(exception);
		Assert.Equal(stream, ex.StreamName);
		Assert.Equal("groupname123", ex.GroupName);
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_link_to_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		EventData[]                events;
		TaskCompletionSource<bool> eventsReceived     = new();
		int                        eventReceivedCount = 0;

		events = Fixture.CreateTestEvents(eventWriteCount)
			.Select(
				(e, i) => new EventData(
					e.EventId,
					SystemEventTypes.LinkTo,
					Encoding.UTF8.GetBytes($"{i}@{stream}"),
					contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
				)
			)
			.ToArray();

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, new[] { e });

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (subscription, e, retryCount, ct) => {
				await subscription.Ack(e);

				if (Interlocked.Increment(ref eventReceivedCount) == events.Length)
					eventsReceived.TrySetResult(true);
			},
			(s, r, e) => {
				if (e != null)
					eventsReceived.TrySetException(e);
			},
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		await eventsReceived.Task.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_normal_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		int eventReceivedCount = 0;

		var                        events         = Fixture.CreateTestEvents(eventWriteCount).ToArray();
		TaskCompletionSource<bool> eventsReceived = new();

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, new[] { e });

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (subscription, e, retryCount, ct) => {
				await subscription.Ack(e);

				if (Interlocked.Increment(ref eventReceivedCount) == events.Length)
					eventsReceived.TrySetResult(true);
			},
			(s, r, e) => {
				if (e != null)
					eventsReceived.TrySetException(e);
			},
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		await eventsReceived.Task.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_writing_and_subscribing_to_normal_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		int eventReceivedCount = 0;

		var                        events         = Fixture.CreateTestEvents(eventWriteCount).ToArray();
		TaskCompletionSource<bool> eventsReceived = new();
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (subscription, e, retryCount, ct) => {
				await subscription.Ack(e);
				if (Interlocked.Increment(ref eventReceivedCount) == events.Length)
					eventsReceived.TrySetResult(true);
			},
			(s, r, e) => {
				if (e != null)
					eventsReceived.TrySetException(e);
			},
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await eventsReceived.Task.WithTimeout();
	}

	[RetryFact]
	public async Task update_existing_with_check_point() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		StreamPosition checkPoint = default;

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> droppedSource  = new();
		TaskCompletionSource<ResolvedEvent>                           resumedSource  = new();
		TaskCompletionSource<bool>                                    appeared       = new();
		List<ResolvedEvent>                                           appearedEvents = [];
		EventData[]                                                   events         = Fixture.CreateTestEvents(5).ToArray();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(
				checkPointLowerBound: 5,
				checkPointAfter: TimeSpan.FromSeconds(1),
				startFrom: StreamPosition.Start
			),
			userCredentials: TestCredentials.Root
		);

		var checkPointStream = $"$persistentsubscription-{stream}::{group}-checkpoint";

		await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (s, e, _, _) => {
				appearedEvents.Add(e);
				await s.Ack(e);

				if (appearedEvents.Count == events.Length)
					appeared.TrySetResult(true);
			},
			(_, reason, ex) => droppedSource.TrySetResult((reason, ex)),
			TestCredentials.Root
		);

		await Task.WhenAll(appeared.Task.WithTimeout(), Checkpointed());

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await droppedSource.Task.WithTimeout();

		await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (s, e, _, _) => {
				resumedSource.TrySetResult(e);
				await s.Ack(e);
			},
			(_, reason, ex) => {
				if (ex is not null)
					resumedSource.TrySetException(ex);
				else
					resumedSource.TrySetResult(default);
			},
			TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1));

		var resumedEvent = await resumedSource.Task.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(checkPoint.Next(), resumedEvent.Event.EventNumber);

		return;

		async Task Checkpointed() {
			await using var subscription = Fixture.Streams.SubscribeToStream(
				checkPointStream,
				FromStream.Start,
				userCredentials: TestCredentials.Root
			);

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				checkPoint = resolvedEvent.Event.Data.ParseStreamPosition();
				return;
			}

			throw new InvalidOperationException();
		}
	}

	[RetryFact]
	public async Task update_existing_with_subscribers() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> droppedSource = new();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			delegate { return Task.CompletedTask; },
			(_, reason, ex) => droppedSource.TrySetResult((reason, ex)),
			TestCredentials.Root
		);

		await Fixture.Subscriptions.UpdateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var dropped = droppedSource.Task;

		var (reason, exception) = await dropped.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task when_writing_and_subscribing_to_normal_events_manual_nack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		int eventReceivedCount = 0;

		EventData[] events = Fixture.CreateTestEvents(eventWriteCount)
			.ToArray();

		TaskCompletionSource<bool> eventsReceived = new();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToStreamAsync(
			stream,
			group,
			async (subscription, e, retryCount, ct) => {
				await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "fail", e);

				if (Interlocked.Increment(ref eventReceivedCount) == events.Length)
					eventsReceived.TrySetResult(true);
			},
			(s, r, e) => {
				if (e != null)
					eventsReceived.TrySetException(e);
			},
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await eventsReceived.Task.WithTimeout();
	}
}
