using EventStore.Client;
using Kurrent.Client.Tests.Streams;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:Streams")]
public class SubscribeToAllTests(ITestOutputHelper output, SubscribeToAllTests.CustomFixture fixture)
	: KurrentTemporaryTests<SubscribeToAllTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task receives_all_events_from_start() {
		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		await using var subscription = Fixture.Streams.SubscribeToAll(FromAll.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

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
	public async Task receives_all_events_from_end() {
		var seedEvents = Fixture.CreateTestEvents(10).ToArray();

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await using var subscription = Fixture.Streams.SubscribeToAll(FromAll.End);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		// add the events we want to receive after we start the subscription
		foreach (var evt in seedEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
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
	public async Task receives_all_events_from_position() {
		var seedEvents = Fixture.CreateTestEvents(10).ToArray();
		var pageSize   = seedEvents.Length / 2;

		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		IWriteResult writeResult = new SuccessResult();
		foreach (var evt in seedEvents.Take(pageSize))
			writeResult = await Fixture.Streams.AppendToStreamAsync(
				$"stream-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		var position = FromAll.After(writeResult.LogPosition);

		await using var subscription = Fixture.Streams.SubscribeToAll(position);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"stream-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

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
	public async Task receives_all_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await using var subscription = Fixture.Streams.SubscribeToAll(FromAll.Start, true);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

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

	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType = typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_start(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_start)}-{filter.Name}-{Guid.NewGuid():N}";

		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);

		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();

		var pageSize = seedEvents.Length / 2;

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(3)
		);

		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start)
			.Messages.CountAsync();

		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);

		// Debugging:
		// await foreach (var evt in Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start))
		// 	Fixture.Log.Debug("Read event {EventId} from {StreamId}.", evt.OriginalEvent.EventId, evt.OriginalEvent.EventStreamId);

		// add some of the events we want to see before we start the subscription
		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1);

		await using var subscription = Fixture.Streams.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		// add some of the events we want to see after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		bool checkpointReached = false;

		await Subscribe().WithTimeout();

		Assert.True(checkpointReached);

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				switch (enumerator.Current) {
					case StreamMessage.AllStreamCheckpointReached:
						checkpointReached = true;

						break;

					case StreamMessage.Event(var resolvedEvent): {
						availableEvents.Remove(resolvedEvent.Event.EventId);

						if (availableEvents.Count == 0) {
							return;
						}

						break;
					}
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType = typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_end(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_end)}-{filter.Name}-{Guid.NewGuid():N}";

		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);

		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();

		var pageSize = seedEvents.Length / 2;

		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(3)
		);

		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start)
			.Messages.CountAsync();

		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);

		// Debugging:
		// await foreach (var evt in Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start))
		// 	Fixture.Log.Debug("Read event {EventId} from {StreamId}.", evt.OriginalEvent.EventId, evt.OriginalEvent.EventStreamId);

		// add some of the events we want to see before we start the subscription
		foreach (var evt in seedEvents.Take(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1);

		await using var subscription = Fixture.Streams.SubscribeToAll(FromAll.End, filterOptions: filterOptions);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		// add some of the events we want to see after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		bool checkpointReached = false;

		await Subscribe().WithTimeout();

		Assert.True(checkpointReached);

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				switch (enumerator.Current) {
					case StreamMessage.AllStreamCheckpointReached:
						checkpointReached = true;

						break;

					case StreamMessage.Event(var resolvedEvent): {
						availableEvents.Remove(resolvedEvent.Event.EventId);

						if (availableEvents.Count == 0) {
							return;
						}

						break;
					}
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(SubscriptionFilter.TestCases), MemberType = typeof(SubscriptionFilter))]
	public async Task receives_all_filtered_events_from_position(SubscriptionFilter filter) {
		var streamPrefix = $"{nameof(receives_all_filtered_events_from_position)}-{filter.Name}-{Guid.NewGuid():N}";

		Fixture.Log.Information("Using filter {FilterName} with prefix {StreamPrefix}", filter.Name, streamPrefix);

		var seedEvents = Fixture.CreateTestEvents(64)
			.Select(evt => filter.PrepareEvent(streamPrefix, evt))
			.ToArray();

		var pageSize = seedEvents.Length / 2;

		// only the second half of the events will be received
		var availableEvents = new HashSet<Uuid>(seedEvents.Skip(pageSize).Select(x => x.EventId));

		// add noise
		await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(3)
		);

		var existingEventsCount = await Fixture.Streams.ReadAllAsync(Direction.Forwards, Position.Start)
			.Messages.CountAsync();

		Fixture.Log.Debug("Existing events count: {ExistingEventsCount}", existingEventsCount);

		// add some of the events that are a match to the filter but will not be received
		IWriteResult writeResult = new SuccessResult();
		foreach (var evt in seedEvents.Take(pageSize))
			writeResult = await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		var position = FromAll.After(writeResult.LogPosition);

		var filterOptions = new SubscriptionFilterOptions(filter.Create(streamPrefix), 1);

		await using var subscription = Fixture.Streams.SubscribeToAll(position, filterOptions: filterOptions);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		// add the events we want to receive after we start the subscription
		foreach (var evt in seedEvents.Skip(pageSize))
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}-{evt.EventId.ToGuid():N}",
				StreamState.NoStream,
				new[] { evt }
			);

		bool checkpointReached = false;

		await Subscribe().WithTimeout();

		Assert.True(checkpointReached);

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				switch (enumerator.Current) {
					case StreamMessage.AllStreamCheckpointReached:
						checkpointReached = true;

						break;

					case StreamMessage.Event(var resolvedEvent): {
						availableEvents.Remove(resolvedEvent.Event.EventId);

						if (availableEvents.Count == 0) {
							return;
						}

						break;
					}
				}
			}
		}
	}

	[Fact]
	public async Task receives_all_filtered_events_with_resolved_links() {
		var streamName = Fixture.GetStreamName();

		var seedEvents      = Fixture.CreateTestEvents(3).ToArray();
		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		var filterOptions = new SubscriptionFilterOptions(StreamFilter.Prefix($"$et-{KurrentPermanentFixture.TestEventType}"));

		await using var subscription =
			Fixture.Streams.SubscribeToAll(FromAll.Start, true, filterOptions: filterOptions);

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent) ||
				    !resolvedEvent.OriginalEvent.EventStreamId.StartsWith($"$et-{KurrentPermanentFixture.TestEventType}")) {
					continue;
				}

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count == 0) {
					return;
				}
			}
		}
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) {
			OnSetup = async () => {
				await Streams.SetStreamMetadataAsync(
					SystemStreams.AllStream,
					StreamState.NoStream,
					new(acl: new(SystemRoles.All)),
					userCredentials: TestCredentials.Root
				);

				await Streams.AppendToStreamAsync($"SubscriptionsFixture-Noise-{Guid.NewGuid():N}", StreamState.NoStream, CreateTestEvents(10));
			};
		}
	}
}
