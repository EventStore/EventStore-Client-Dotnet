namespace EventStore.Client.Streams.Tests.Subscriptions;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:Stream")]
public class subscribe_to_stream(ITestOutputHelper output, SubscriptionsFixture fixture)
	: EventStoreTests<SubscriptionsFixture>(output, fixture) {
	[Fact]
	public async Task receives_all_events_from_start() {
		var streamName = Fixture.GetStreamName();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, seedEvents.Skip(pageSize));

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}

	[Fact]
	public async Task receives_all_events_from_position() {
		var streamName = Fixture.GetStreamName();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		var writeResult =
			await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents.Take(pageSize));

		var streamPosition = StreamPosition.FromStreamRevision(writeResult.NextExpectedStreamRevision);
		var checkpoint     = FromStream.After(streamPosition);

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, checkpoint);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			writeResult.NextExpectedStreamRevision,
			seedEvents.Skip(pageSize)
		);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.OriginalEvent.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}

	[Fact]
	public async Task receives_all_events_from_non_existing_stream() {
		var streamName = Fixture.GetStreamName();

		var seedEvents = Fixture.CreateTestEvents(10).ToArray();

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.OriginalEvent.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}

	[Fact]
	public async Task allow_multiple_subscriptions_to_same_stream() {
		var streamName = Fixture.GetStreamName();

		var seedEvents = Fixture.CreateTestEvents(5).ToArray();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await using var subscription1 = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator1   = subscription1.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator1.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator1.Current);

		await using var subscription2 = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator2   = subscription2.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator2.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator2.Current);

		await Task.WhenAll(Subscribe(enumerator1), Subscribe(enumerator2)).WithTimeout();

		return;

		async Task Subscribe(IAsyncEnumerator<StreamMessage> subscription) {
			var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

			while (await subscription.MoveNextAsync()) {
				if (subscription.Current is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.OriginalEvent.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}

	[Fact]
	public async Task drops_when_stream_tombstoned() {
		var streamName = Fixture.GetStreamName();

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		// rest in peace
		await Fixture.Streams.TombstoneAsync(streamName, StreamState.NoStream);

		var ex = await Assert.ThrowsAsync<StreamDeletedException>(
			async () => {
				while (await enumerator.MoveNextAsync()) { }
			}
		).WithTimeout();

		ex.ShouldBeOfType<StreamDeletedException>().Stream.ShouldBe(streamName);
	}

	[Fact]
	public async Task receives_all_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await using var subscription =
			Fixture.Streams.SubscribeToStream($"$et-{EventStoreFixture.TestEventType}", FromStream.Start, true);

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent) ||
				    !resolvedEvent.OriginalEvent.EventStreamId.StartsWith($"$et-{EventStoreFixture.TestEventType}")) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}
}
