namespace EventStore.Client.Streams.Tests;

[DedicatedDatabase]
public class subscribe_to_all_filtered : IClassFixture<subscribe_to_all_filtered.CustomFixture> {
	public subscribe_to_all_filtered(ITestOutputHelper output, CustomFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	CustomFixture Fixture { get; }

	public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] { filter });

	[Theory]
	[MemberData(nameof(FilterCases))]
	public async Task reads_all_existing_events(string filterName) {
		var streamPrefix = Fixture.GetStreamName();
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);

		var appeared       = new TaskCompletionSource<bool>();
		var dropped        = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		var checkpointSeen = new TaskCompletionSource<bool>();
		var filter         = getFilter(streamPrefix);

		var events = Fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e))
			.ToArray();

		using var enumerator = events.OfType<EventData>().GetEnumerator();
		enumerator.MoveNext();

		await Fixture.Streams.AppendToStreamAsync(
			Guid.NewGuid().ToString(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(256)
		);

		foreach (var e in events)
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.Start,
				EventAppeared,
				false,
				SubscriptionDropped,
				new(filter, 5, CheckpointReached)
			)
			.WithTimeout();

		await Task.WhenAll(appeared.Task, checkpointSeen.Task).WithTimeout();

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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
			dropped.SetResult((reason, ex));
			if (reason != SubscriptionDroppedReason.Disposed) {
				appeared.TrySetException(ex!);
				checkpointSeen.TrySetException(ex!);
			}
		}

		Task CheckpointReached(StreamSubscription _, Position position, CancellationToken ct) {
			checkpointSeen.TrySetResult(true);

			return Task.CompletedTask;
		}
	}

	[Theory]
	[MemberData(nameof(FilterCases))]
	public async Task reads_all_existing_events_and_keep_listening_to_new_ones(string filterName) {
		var streamPrefix = Fixture.GetStreamName();
		var (getFilter, prepareEvent) = Filters.GetFilter(filterName);

		var appeared       = new TaskCompletionSource<bool>();
		var dropped        = new TaskCompletionSource<(SubscriptionDroppedReason, Exception?)>();
		var checkpointSeen = new TaskCompletionSource<bool>();
		var filter         = getFilter(streamPrefix);
		var events = Fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e))
			.ToArray();

		var beforeEvents = events.Take(10);
		var afterEvents  = events.Skip(10);

		using var enumerator = events.OfType<EventData>().GetEnumerator();
		enumerator.MoveNext();

		await Fixture.Streams.AppendToStreamAsync(
			Guid.NewGuid().ToString(),
			StreamState.NoStream,
			Fixture.CreateTestEvents(256)
		);

		foreach (var e in beforeEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		using var subscription = await Fixture.Streams.SubscribeToAllAsync(
				FromAll.Start,
				EventAppeared,
				false,
				SubscriptionDropped,
				new(filter, 5, CheckpointReached)
			)
			.WithTimeout();

		foreach (var e in afterEvents)
			await Fixture.Streams.AppendToStreamAsync(
				$"{streamPrefix}_{Guid.NewGuid():n}",
				StreamState.NoStream,
				new[] { e }
			);

		await Task.WhenAll(appeared.Task, checkpointSeen.Task).WithTimeout();

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

		void SubscriptionDropped(StreamSubscription s, SubscriptionDroppedReason reason, Exception? ex) {
			dropped.SetResult((reason, ex));
			if (reason != SubscriptionDroppedReason.Disposed) {
				appeared.TrySetException(ex!);
				checkpointSeen.TrySetException(ex!);
			}
		}

		Task CheckpointReached(StreamSubscription _, Position position, CancellationToken ct) {
			checkpointSeen.TrySetResult(true);

			return Task.CompletedTask;
		}
	}

	public class CustomFixture : EventStoreFixture {
		public const string FilteredOutStream = nameof(FilteredOutStream);

		public CustomFixture() {
			OnSetup = async () => {
				await Streams.SetStreamMetadataAsync(
					SystemStreams.AllStream,
					StreamState.NoStream,
					new(acl: new(SystemRoles.All)),
					userCredentials: TestCredentials.Root
				);

				await Streams.AppendToStreamAsync(FilteredOutStream, StreamState.NoStream, CreateTestEvents(10));
			};
		}
	}
}