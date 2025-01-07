using System.Text;
using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class SubscribeToAllTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task can_create_duplicate_name_on_different_streams() {
		// Arrange
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		// Act
		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);
	}

	[RetryFact]
	public async Task connect_to_existing_with_max_one_client() {
		// Arrange
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(maxSubscriberCount: 1), userCredentials: TestCredentials.Root);

		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(() => Task.WhenAll(Subscribe().WithTimeout(), Subscribe().WithTimeout()));

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
			await subscription.Messages.AnyAsync();
		}
	}

	[RetryFact]
	public async Task connect_to_existing_with_permissions() {
		// Arrange
		var group = Fixture.GetGroupName();
		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		// Act
		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		// Assert
		subscription.Messages
			.FirstAsync().AsTask().WithTimeout()
			.GetAwaiter().GetResult()
			.ShouldBeOfType<PersistentSubscriptionMessage.SubscriptionConfirmation>();
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning() {
		var group = Fixture.GetGroupName();

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

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(events[0].Event.EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		foreach (var @event in Fixture.CreateTestEvents(10))
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<TimeoutException>(
			() => subscription.Messages
				.OfType<PersistentSubscriptionMessage.Event>()
				.Where(e => !SystemStreams.IsSystemStream(e.ResolvedEvent.OriginalStreamId))
				.AnyAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(250))
		);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set_then_event_written() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		var expectedStreamId = Guid.NewGuid().ToString();
		var expectedEvent    = Fixture.CreateTestEvents(1).First();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await Fixture.Streams.AppendToStreamAsync(expectedStreamId, StreamState.NoStream, [expectedEvent]);

		var resolvedEvent = await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.Where(resolvedEvent => !SystemStreams.IsSystemStream(resolvedEvent.OriginalStreamId))
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(expectedEvent.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<TimeoutException>(
			() => subscription.Messages
				.OfType<PersistentSubscriptionMessage.Event>()
				.Where(e => !SystemStreams.IsSystemStream(e.ResolvedEvent.OriginalStreamId))
				.AnyAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(250))
		);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_then_event_written() {
		var stream           = Fixture.GetStreamName();
		var group            = Fixture.GetGroupName();
		var expectedStreamId = Fixture.GetStreamName();
		var expectedEvent    = Fixture.CreateTestEvents().First();

		foreach (var @event in Fixture.CreateTestEvents(10)) {
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.End), userCredentials: TestCredentials.Root);
		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await Fixture.Streams.AppendToStreamAsync(expectedStreamId, StreamState.NoStream, [expectedEvent]);

		var resolvedEvent = await subscription!.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.Where(resolvedEvent => !SystemStreams.IsSystemStream(resolvedEvent.OriginalStreamId))
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(expectedEvent.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_invalid_middle_position() {
		var group = Fixture.GetGroupName();

		var invalidPosition = new Position(1L, 1L);
		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: invalidPosition),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var enumerator   = subscription.Messages.GetAsyncEnumerator();

		await enumerator.MoveNextAsync();
		var ex = await Assert.ThrowsAsync<PersistentSubscriptionDroppedByServerException>(
			async () =>
				await enumerator!.MoveNextAsync()
		);

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_valid_middle_position() {
		var group = Fixture.GetGroupName();

		var events = await Fixture.Streams
			.ReadAllAsync(Direction.Forwards, Position.Start, 10, userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		var expectedEvent = events[events.Length / 2]; //just a random event in the middle of the results

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: expectedEvent.OriginalPosition),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		var resolvedEvent = await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(expectedEvent.OriginalPosition, resolvedEvent.Event.Position);
		Assert.Equal(expectedEvent.Event.EventId, resolvedEvent.Event.EventId);
		Assert.Equal(expectedEvent.Event.EventStreamId, resolvedEvent.Event.EventStreamId);
	}

	[RetryFact]
	public async Task connect_with_retries() {
		// Arrange
		var group = Fixture.GetGroupName();
		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

		// Act
		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var retryCount = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.SelectAwait(
				async e => {
					if (e.RetryCount > 4) {
						await subscription.Ack(e.ResolvedEvent);
					} else {
						await subscription.Nack(
							PersistentSubscriptionNakEventAction.Retry,
							"Not yet tried enough times",
							e.ResolvedEvent
						);
					}

					return e.RetryCount;
				}
			)
			.Where(retryCount => retryCount > 4)
			.FirstOrDefaultAsync()
			.AsTask()
			.WithTimeout();

		// Assert
		retryCount.ShouldBe(5);
	}

	[RetryFact]
	public async Task create_after_deleting_the_same() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		await Fixture.Subscriptions.DeleteToAllAsync(group, userCredentials: TestCredentials.Root);
		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
	}

	[RetryFact]
	public async Task create_duplicate() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var ex = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Subscriptions.CreateToAllAsync(
				group,
				new(),
				userCredentials: TestCredentials.Root
			)
		);

		ex.StatusCode.ShouldBe(StatusCode.AlreadyExists);
	}

	[RetryFact]
	public async Task create_with_commit_position_equal_to_last_indexed_position() {
		// Arrange
		var group = Fixture.GetGroupName();

		// Act
		var lastEvent = await Fixture.Streams.ReadAllAsync(
			Direction.Backwards,
			Position.End,
			1,
			userCredentials: TestCredentials.Root
		).FirstAsync();

		var lastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: new Position(lastCommitPosition, lastCommitPosition)),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public Task create_with_prepare_position_larger_than_commit_position() {
		var group = Fixture.GetGroupName();

		return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() =>
				Fixture.Subscriptions.CreateToAllAsync(
					group,
					new(startFrom: new Position(0, 1)),
					userCredentials: TestCredentials.Root
				)
		);
	}

	[RetryFact]
	public async Task deleting_existing_with_subscriber() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		await Fixture.Subscriptions.DeleteToAllAsync(group, userCredentials: TestCredentials.Root);

		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		Assert.True(
			await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
				.AsTask()
				.WithTimeout()
		);
	}

	[RetryFact]
	public async Task deleting_existing_with_permissions() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.DeleteToAllAsync(
			group,
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task deleting_filtered() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			EventTypeFilter.Prefix("prefix-filter-"),
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.DeleteToAllAsync(group, userCredentials: TestCredentials.Root);
	}

	[RetryFact]
	public async Task deleting_nonexistent() {
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() => Fixture.Subscriptions.DeleteToAllAsync(Guid.NewGuid().ToString(), userCredentials: TestCredentials.Root)
		);
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_link_to_events_manual_ack() {
		var group           = Fixture.GetGroupName();
		var bufferCount     = 10;
		var eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount)
			.Select(
				(e, i) => new EventData(
					e.EventId,
					SystemEventTypes.LinkTo,
					Encoding.UTF8.GetBytes($"{i}@test"),
					contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
				)
			)
			.ToArray();

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
		}

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: Position.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, bufferSize: bufferCount, userCredentials: TestCredentials.Root);
		await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(e => subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_normal_events_manual_ack() {
		var group           = Fixture.GetGroupName();
		var stream          = Fixture.GetStreamName();
		var bufferCount     = 10;
		var eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: Position.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, bufferSize: bufferCount, userCredentials: TestCredentials.Root);
		await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(e => subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_writing_and_subscribing_to_normal_events_manual_ack() {
		var group           = Fixture.GetGroupName();
		var bufferCount     = 10;
		var eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(startFrom: Position.End, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, bufferSize: bufferCount, userCredentials: TestCredentials.Root);
		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
		}

		await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.SelectAwait(
				async e => {
					await subscription.Ack(e.ResolvedEvent);
					return e;
				}
			)
			.Where(e => e.ResolvedEvent.OriginalStreamId.StartsWith("test-"))
			.Take(events.Length)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout();
	}

	[RetryFact]
	public async Task update_existing() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.UpdateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task update_existing_filtered() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			EventTypeFilter.Prefix("prefix-filter-"),
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.UpdateToAllAsync(
			group,
			new(true),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task update_existing_with_check_point() {
		var      group          = Fixture.GetGroupName();
		var      events         = Fixture.CreateTestEvents(5).ToArray();
		var      appearedEvents = new List<ResolvedEvent>();
		Position checkPoint     = default;

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, [e]);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: Position.Start),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var enumerator   = subscription.Messages.GetAsyncEnumerator();
		await enumerator.MoveNextAsync();

		await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoint().WithTimeout());

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		try {
			while (await enumerator!.MoveNextAsync()) { }
		} catch (PersistentSubscriptionDroppedByServerException) { }

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, new[] { e });
		}

		await using var sub = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var resumed = await sub.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.Take(1)
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.True(resumed.Event.Position > checkPoint);

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
					continue;
				}

				appearedEvents.Add(resolvedEvent);
				await subscription.Ack(resolvedEvent);
				if (appearedEvents.Count == events.Length) {
					break;
				}
			}
		}

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
		List<ResolvedEvent> appearedEvents = [];
		var                 events         = Fixture.CreateTestEvents(5).ToArray();
		var                 group          = Fixture.GetGroupName();
		Position            checkPoint     = default;

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, [e]);
		}

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			StreamFilter.Prefix("test"),
			new(checkPointLowerBound: 5, checkPointAfter: TimeSpan.FromSeconds(1), startFrom: Position.Start),
			userCredentials: TestCredentials.Root
		);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var enumerator   = subscription.Messages.GetAsyncEnumerator();
		await enumerator.MoveNextAsync();

		await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoint().WithTimeout());

		// Force restart of the subscription
		await Fixture.Subscriptions.UpdateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		try {
			while (await enumerator.MoveNextAsync()) { }
		} catch (PersistentSubscriptionDroppedByServerException) { }

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.NoStream, [e]);
		}

		await using var sub = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		var resumed = await sub.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.Take(1)
			.FirstAsync()
			.AsTask()
			.WithTimeout();

		Assert.True(resumed.Event.Position > checkPoint);

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
					continue;
				}

				appearedEvents.Add(resolvedEvent);
				await subscription.Ack(resolvedEvent);
				if (appearedEvents.Count == events.Length) {
					break;
				}
			}
		}

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
	public async Task update_existing_with_commit_position_equal_to_last_indexed_position() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var lastEvent = await Fixture.Streams.ReadAllAsync(
			Direction.Backwards,
			Position.End,
			1,
			userCredentials: TestCredentials.Root
		).FirstAsync();

		var lastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
		await Fixture.Subscriptions.UpdateToAllAsync(
			group,
			new(startFrom: new Position(lastCommitPosition, lastCommitPosition)),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task update_existing_with_subscribers() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(startFrom: Position.Start), userCredentials: TestCredentials.Root);

		var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		var enumerator = subscription.Messages.GetAsyncEnumerator();

		await enumerator.MoveNextAsync();
		await Fixture.Subscriptions.UpdateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		var ex = await Assert.ThrowsAsync<PersistentSubscriptionDroppedByServerException>(
			async () => {
				while (await enumerator.MoveNextAsync()) { }
			}
		).WithTimeout();

		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[RetryFact]
	public async Task when_writing_and_filtering_out_events() {
		var events = Fixture.CreateTestEvents(10).ToArray();
		var group  = Fixture.GetGroupName();
		var prefix = Guid.NewGuid().ToString("N");

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

		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		await Task.WhenAll(Subscribe().WithTimeout(), WaitForCheckpoints().WithTimeout());

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

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, _)) {
					continue;
				}

				appearedEvents.Add(resolvedEvent);
				await subscription.Ack(resolvedEvent);
				if (appearedEvents.Count == events.Length) {
					break;
				}
			}
		}

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

	[RetryFact]
	public async Task create_with_commit_position_larger_than_last_indexed_position() {
		var group = Fixture.GetGroupName();

		var lastEvent = await Fixture.Streams.ReadAllAsync(
			Direction.Backwards,
			Position.End,
			1,
			userCredentials: TestCredentials.Root
		).FirstAsync();

		var lastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
		var ex = await Assert.ThrowsAsync<RpcException>(
			() =>
				Fixture.Subscriptions.CreateToAllAsync(
					group,
					new(startFrom: new Position(lastCommitPosition + 1, lastCommitPosition)),
					userCredentials: TestCredentials.Root
				)
		);

		Assert.Equal(StatusCode.Internal, ex.StatusCode);
	}
}
