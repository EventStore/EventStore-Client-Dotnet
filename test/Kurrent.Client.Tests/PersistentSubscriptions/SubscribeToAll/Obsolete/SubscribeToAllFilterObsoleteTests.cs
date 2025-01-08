using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllFilterObsoleteTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryTheory]
	[MemberData(nameof(FilterCases))]
	public async Task happy_case_filtered_reads_all_existing_filtered_events(string filterName) {
		var streamPrefix = $"{filterName}-{Fixture.GetStreamName()}";
		var group        = Fixture.GetGroupName();
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
		var filter = getFilter(streamPrefix);

		await Fixture.Streams.AppendToStreamAsync(
			Guid.NewGuid().ToString(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(256)
		);

		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.Any,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		var appearedEvents = new List<EventRecord>();
		var events         = Fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();

		foreach (var e in events) {
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				[e]
			);
		}

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			filter,
			new(startFrom: Position.Start),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await subscription.Messages
			.OfType<PersistentSubscriptionMessage.Event>()
			.Take(events.Length)
			.ForEachAwaitAsync(
				async e => {
					var (resolvedEvent, _) = e;
					appearedEvents.Add(resolvedEvent.Event);
					await subscription.Ack(resolvedEvent);
				}
			)
			.WithTimeout();

		Assert.Equal(events.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
	}

	[RetryTheory]
	[MemberData(nameof(FilterCases))]
	public async Task happy_case_filtered_with_start_from_set(string filterName) {
		var group        = Fixture.GetGroupName();
		var streamPrefix = $"{filterName}-{Fixture.GetStreamName()}";
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
		var filter = getFilter(streamPrefix);

		var events          = Fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();
		var eventsToSkip    = events.Take(10).ToArray();
		var eventsToCapture = events.Skip(10).ToArray();

		IWriteResult? eventToCaptureResult = null;

		await Fixture.Streams.AppendToStreamAsync(
			Guid.NewGuid().ToString(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(256)
		);

		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.Any,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		foreach (var e in eventsToSkip) {
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);
		}

		foreach (var e in eventsToCapture) {
			var result = await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

			eventToCaptureResult ??= result;
		}

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			filter,
			new(startFrom: eventToCaptureResult!.LogPosition),
			userCredentials: TestCredentials.Root
		);

		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		var appearedEvents = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(10)
			.Select(e => e.ResolvedEvent.Event)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(eventsToCapture.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
	}

	public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] { filter });
}
