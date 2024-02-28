namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class happy_case_filtered_with_start_from_set : IClassFixture<happy_case_filtered_with_start_from_set.Fixture> {
	private readonly Fixture _fixture;

	public happy_case_filtered_with_start_from_set(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] { filter });

	[SupportsPSToAll.Theory]
	[MemberData(nameof(FilterCases))]
	public async Task reads_all_existing_filtered_events_from_specified_start(string filterName) {
		var streamPrefix = $"{filterName}-{_fixture.GetStreamName()}";
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
		var filter = getFilter(streamPrefix);

		var events = _fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();
		var eventsToSkip = events.Take(10).ToArray();
		var eventsToCapture = events.Skip(10).ToArray();

		IWriteResult? eventToCaptureResult = null;

		foreach (var e in eventsToSkip) {
			await _fixture.StreamsClient.AppendToStreamAsync($"{streamPrefix}_{Guid.NewGuid():n}", StreamState.NoStream,
				new[] { e });
		}

		foreach (var e in eventsToCapture) {
			var result = await _fixture.StreamsClient.AppendToStreamAsync($"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream, new[] { e });

			eventToCaptureResult ??= result;
		}

		await _fixture.Client.CreateToAllAsync(filterName, filter, new(startFrom: eventToCaptureResult!.LogPosition),
			userCredentials: TestCredentials.Root);

		await using var subscription =
			_fixture.Client.SubscribeToAll(filterName, userCredentials: TestCredentials.Root);

		var appearedEvents = await subscription.Messages.OfType<PersistentSubscriptionMessage.Event>()
			.Take(10)
			.Select(e => e.ResolvedEvent.Event)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout();

		Assert.Equal(eventsToCapture.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
	}

	public class Fixture : EventStoreClientFixture {
		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(Guid.NewGuid().ToString(), StreamState.NoStream,
				CreateTestEvents(256));

			await StreamsClient.SetStreamMetadataAsync(SystemStreams.AllStream, StreamState.Any,
				new(acl: new(SystemRoles.All)), userCredentials: TestCredentials.Root);
		}

		protected override Task When() => Task.CompletedTask;
	}
}
