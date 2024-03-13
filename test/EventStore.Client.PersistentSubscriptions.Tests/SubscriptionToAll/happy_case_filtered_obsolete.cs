namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

[Obsolete]
public class happy_case_filtered_obsolete : IClassFixture<happy_case_filtered_obsolete.Fixture> {
	readonly Fixture _fixture;

	public happy_case_filtered_obsolete(Fixture fixture) => _fixture = fixture;

	public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] { filter });

	[SupportsPSToAll.Theory]
	[MemberData(nameof(FilterCases))]
	public async Task reads_all_existing_filtered_events(string filterName) {
		var streamPrefix = $"{filterName}-{_fixture.GetStreamName()}";
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
		var filter = getFilter(streamPrefix);

		var appeared       = new TaskCompletionSource<bool>();
		var appearedEvents = new List<EventRecord>();
		var events         = _fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();

		foreach (var e in events)
			await _fixture.StreamsClient.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		await _fixture.Client.CreateToAllAsync(
			filterName,
			filter,
			new(startFrom: Position.Start),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await _fixture.Client.SubscribeToAllAsync(
				filterName,
				async (s, e, r, ct) => {
					appearedEvents.Add(e.Event);
					if (appearedEvents.Count >= events.Length)
						appeared.TrySetResult(true);

					await s.Ack(e);
				},
				userCredentials: TestCredentials.Root
			)
			.WithTimeout();

		await Task.WhenAll(appeared.Task).WithTimeout();

		Assert.Equal(events.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
	}

	public class Fixture : EventStoreClientFixture {
		protected override async Task Given() {
			await StreamsClient.AppendToStreamAsync(
				Guid.NewGuid().ToString(),
				StreamState.NoStream,
				CreateTestEvents(256)
			);

			await StreamsClient.SetStreamMetadataAsync(
				SystemStreams.AllStream,
				StreamState.Any,
				new(acl: new(SystemRoles.All)),
				userCredentials: TestCredentials.Root
			);
		}

		protected override Task When() => Task.CompletedTask;
	}
}
