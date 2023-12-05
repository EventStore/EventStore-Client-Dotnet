namespace EventStore.Client.Streams.Tests;

[DedicatedDatabase]
public class subscribe_to_all_filtered_live : IClassFixture<EventStoreFixture> {
	public subscribe_to_all_filtered_live(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] { filter });

	[Theory]
	[MemberData(nameof(FilterCases))]
	public async Task does_not_read_all_events_but_keep_listening_to_new_ones(string filterName) {
		const string filteredOutStream = nameof(filteredOutStream);

		await Fixture.Streams.SetStreamMetadataAsync(
			SystemStreams.AllStream,
			StreamState.Any,
			new(acl: new(SystemRoles.All)),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(
			filteredOutStream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(10)
		);

		var streamPrefix = Fixture.GetStreamName();
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);

		var appeared = new TaskCompletionSource<bool>();
		var dropped  = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();

		var filter = getFilter(streamPrefix);
		var events = Fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e))
			.ToArray();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = afterEvents.OfType<EventData>().GetEnumerator();
		enumerator.MoveNext();

		foreach (var e in beforeEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.End,
				EventAppeared,
				false,
				SubscriptionDropped,
				new(filter)
			)
			.WithTimeout();

		foreach (var e in afterEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		await appeared.Task.WithTimeout();

		Assert.False(dropped.Task.IsCompleted);

		subscription.Dispose();

		var (reason, ex) = await dropped.Task.WithTimeout();

		Assert.Equal(SubscriptionDroppedReason.Disposed, reason);
		Assert.Null(ex);

		Task EventAppeared(StreamSubscription _, ResolvedEvent e, CancellationToken ct) {
			try {
				Assert.Equal(enumerator.Current.EventId, e.OriginalEvent.EventId);
				if (!enumerator.MoveNext())
					appeared.TrySetResult(true);
			}
			catch (Exception ex) {
				appeared.TrySetException(ex);
				throw;
			}

			return Task.CompletedTask;
		}

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) =>
			dropped.SetResult((reason, ex));
	}
}