using System.Text;
using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task can_create_duplicate_name_on_different_streams() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.CreateToStreamAsync(
			"someother" + stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);
	}

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

		var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(() => Task.WhenAll(Subscribe().WithTimeout(), Subscribe().WithTimeout()));

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);
			await subscription.Messages.AnyAsync();
		}
	}

	[RetryFact]
	public async Task connect_to_existing_with_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		Assert.True(await subscription.Messages.FirstAsync().AsTask().WithTimeout() is PersistentSubscriptionMessage.SubscriptionConfirmation);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning_and_events_in_it() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		var events = Fixture.CreateTestEvents(10).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);
		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(StreamPosition.Start, resolvedEvent.Event.EventNumber);
		Assert.Equal(events[0].EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_beginning_and_no_stream() {
		var stream  = Fixture.GetStreamName();
		var group   = Fixture.GetGroupName();
		var events  = Fixture.CreateTestEvents().ToArray();
		var eventId = events.Single().EventId;

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var resolvedEvent = await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(StreamPosition.Start, resolvedEvent.Event.EventNumber);
		Assert.Equal(eventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set_and_events_in_it_then_event_written() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		var events = Fixture.CreateTestEvents(11).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_and_events_in_it() {
		var events = Fixture.CreateTestEvents(10).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<TimeoutException>(
			() => subscription.Messages.AnyAsync(message => message is PersistentSubscriptionMessage.Event).AsTask().WithTimeout(TimeSpan.FromMilliseconds(250))
		);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_set_to_end_position_and_events_in_it_then_event_written() {
		var events = Fixture.CreateTestEvents(11).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.TestUser1);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_two_and_no_stream() {
		var events = Fixture.CreateTestEvents(3).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		var eventId = events.Last().EventId;

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(2)),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(2), resolvedEvent.Event.EventNumber);
		Assert.Equal(eventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_and_events_in_it() {
		var events = Fixture.CreateTestEvents(10).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(4)),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var resolvedEvent = await subscription.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(4), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Skip(4).First().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_and_events_in_it_then_event_written() {
		var events = Fixture.CreateTestEvents(11).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(10));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(10)),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(9), events.Skip(10));

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(10), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_existing_with_start_from_x_set_higher_than_x_and_events_in_it_then_event_written() {
		var events = Fixture.CreateTestEvents(12).ToArray();
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(11));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(11)),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(10), events.Skip(11));

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(11), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task connect_to_non_existing_with_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);
		Assert.True(
			await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>()
				.AnyAsync()
				.AsTask()
				.WithTimeout()
		);
	}

	[RetryFact]
	public async Task connect_with_retries() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
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

		Assert.Equal(5, retryCount);
	}

	[RetryFact]
	public async Task connecting_to_a_persistent_subscription() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		var events = Fixture.CreateTestEvents(12).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(11));
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: new StreamPosition(11)),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(10), events.Skip(11));

		var resolvedEvent = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Select(e => e.ResolvedEvent)
			.FirstOrDefaultAsync().AsTask().WithTimeout();

		Assert.Equal(new(11), resolvedEvent.Event.EventNumber);
		Assert.Equal(events.Last().EventId, resolvedEvent.Event.EventId);
	}

	[RetryFact]
	public async Task create_after_deleting_the_same() {
		var stream = Fixture.GetStreamName();
		var group  = $"existing-{Fixture.GetGroupName()}";

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents());
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.DeleteToStreamAsync(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task create_duplicate() {
		var stream = Fixture.GetStreamName();
		var group  = $"duplicate-{Fixture.GetGroupName()}";

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		var ex = await Assert.ThrowsAsync<RpcException>(
			() => Fixture.Subscriptions.CreateToStreamAsync(
				stream,
				group,
				new(),
				userCredentials: TestCredentials.Root
			)
		);

		Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
	}

	[RetryFact]
	public async Task create_on_existing_stream() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents());

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task create_on_non_existing_stream() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task create_with_dont_timeout() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(messageTimeout: TimeSpan.Zero),
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task deleting_existing_with_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);
		await Fixture.Subscriptions.DeleteToStreamAsync(stream, group, userCredentials: TestCredentials.Root);
	}

	[RetryFact]
	public async Task deleting_existing_with_subscriber() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await Fixture.Subscriptions.DeleteToStreamAsync(stream, group, userCredentials: TestCredentials.Root);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);

		Assert.True(
			await subscription.Messages.OfType<PersistentSubscriptionMessage.NotFound>().AnyAsync()
				.AsTask()
				.WithTimeout()
		);
	}

	[RetryFact]
	public async Task deleting_nonexistent() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() => Fixture.Subscriptions.DeleteToStreamAsync(stream, group, userCredentials: TestCredentials.Root)
		);
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_link_to_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount)
			.Select(
				(e, i) => new EventData(
					e.EventId,
					SystemEventTypes.LinkTo,
					Encoding.UTF8.GetBytes($"{i}@{stream}"),
					contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
				)
			).ToArray();

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(e => subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_catching_up_to_normal_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, new[] { e });

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		await subscription!.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(e => subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	[RetryFact]
	public async Task happy_case_writing_and_subscribing_to_normal_events_manual_ack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.End, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(
			stream,
			group,
			bufferSize: bufferCount,
			userCredentials: TestCredentials.Root
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(e => subscription.Ack(e.ResolvedEvent))
			.WithTimeout();
	}

	[RetryFact]
	public async Task list_without_persistent_subscriptions_should_throw() {
		var stream = Fixture.GetStreamName();

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () =>
				await Fixture.Subscriptions.ListToStreamAsync(stream, userCredentials: TestCredentials.Root)
		);
	}

	[RetryFact]
	public async Task update_existing() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.UpdateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);
	}

	[RetryFact]
	public async Task update_existing_with_subscribers() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, userCredentials: TestCredentials.Root);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		await enumerator.MoveNextAsync();

		await Fixture.Subscriptions.UpdateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		var ex = await Assert.ThrowsAsync<PersistentSubscriptionDroppedByServerException>(
			async () => {
				while (await enumerator.MoveNextAsync()) { }
			}
		).WithTimeout();

		Assert.Equal(stream, ex.StreamName);
		Assert.Equal(group, ex.GroupName);
	}

	[Regression.Fact(21, "20.x returns the wrong exception")]
	public async Task update_non_existent() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() => Fixture.Subscriptions.UpdateToStreamAsync(
				stream,
				group,
				new(),
				userCredentials: TestCredentials.Root
			)
		);
	}

	[RetryFact]
	public async Task when_writing_and_subscribing_to_normal_events_manual_nack() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		const int bufferCount     = 10;
		const int eventWriteCount = bufferCount * 2;

		var events = Fixture.CreateTestEvents(eventWriteCount).ToArray();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(startFrom: StreamPosition.Start, resolveLinkTos: true),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group, bufferSize: bufferCount, userCredentials: TestCredentials.Root);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, [e]);

		await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(1)
			.ForEachAwaitAsync(
				async message =>
					await subscription.Nack(
						PersistentSubscriptionNakEventAction.Park,
						"fail",
						message.ResolvedEvent
					)
			)
			.WithTimeout();
	}
}
