// ReSharper disable InconsistentNaming

using EventStore.Client;

namespace Kurrent.Client.Tests.Bugs.Obsolete;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Bug")]
[Obsolete("Tests will be removed in future release when older subscriptions APIs are removed from the client")]
public class Issue2544 : IClassFixture<KurrentPermanentFixture> {
	public Issue2544(ITestOutputHelper output, KurrentPermanentFixture fixture) {
		Fixture = fixture.With(x => x.CaptureTestRun(output));

		Seen = Enumerable.Range(0, 1 + Batches * BatchSize)
			.Select(i => new StreamPosition((ulong)i))
			.ToDictionary(r => r, _ => false);

		Completed = new();
	}

	KurrentPermanentFixture Fixture { get; }

	const int BatchSize = 18;
	const int Batches   = 4;

	readonly TaskCompletionSource<bool>       Completed;
	readonly Dictionary<StreamPosition, bool> Seen;

	public static IEnumerable<object?[]> TestCases() => Enumerable.Range(0, 5).Select(i => new object[] { i });

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task subscribe_to_stream(int iteration) {
		var streamName = $"{Fixture.GetStreamName()}_{iteration}";
		var startFrom  = FromStream.Start;

		async Task<StreamSubscription> Subscribe() =>
			await Fixture.Streams
				.SubscribeToStreamAsync(
					streamName,
					startFrom,
					(_, e, _) => EventAppeared(e, streamName, out startFrom),
					false,
					(s, r, e) => SubscriptionDropped(s, r, e, Subscribe)
				);

		using var _ = await Subscribe();

		await AppendEvents(streamName);

		await Completed.Task.WithTimeout();
	}

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task subscribe_to_all(int iteration) {
		var streamName = $"{Fixture.GetStreamName()}_{iteration}";
		var startFrom  = FromAll.Start;

		async Task<StreamSubscription> Subscribe() =>
			await Fixture.Streams
				.SubscribeToAllAsync(
					startFrom,
					(_, e, _) => EventAppeared(e, streamName, out startFrom),
					false,
					(s, r, e) => SubscriptionDropped(s, r, e, Subscribe)
				);

		using var _ = await Subscribe();

		await AppendEvents(streamName);

		await Completed.Task.WithTimeout();
	}

	[Theory]
	[MemberData(nameof(TestCases))]
	public async Task subscribe_to_all_filtered(int iteration) {
		var streamName = $"{Fixture.GetStreamName()}_{iteration}";
		var startFrom  = FromAll.Start;

		async Task<StreamSubscription> Subscribe() =>
			await Fixture.Streams
				.SubscribeToAllAsync(
					startFrom,
					(_, e, _) => EventAppeared(e, streamName, out startFrom),
					false,
					(s, r, e) => SubscriptionDropped(s, r, e, Subscribe),
					new(EventTypeFilter.ExcludeSystemEvents())
				);

		await Subscribe();

		await AppendEvents(streamName);

		await Completed.Task.WithTimeout();
	}

	async Task AppendEvents(string streamName) {
		await Task.Delay(TimeSpan.FromMilliseconds(10));

		var expectedRevision = StreamRevision.None;

		for (var i = 0; i < Batches; i++) {
			if (expectedRevision == StreamRevision.None) {
				var result = await Fixture.Streams.AppendToStreamAsync(
					streamName,
					StreamState.NoStream,
					Fixture.CreateTestEvents(BatchSize)
				);

				expectedRevision = result.NextExpectedStreamRevision;
			} else {
				var result = await Fixture.Streams.AppendToStreamAsync(
					streamName,
					expectedRevision,
					Fixture.CreateTestEvents(BatchSize)
				);

				expectedRevision = result.NextExpectedStreamRevision;
			}

			await Task.Delay(TimeSpan.FromMilliseconds(10));
		}

		await Fixture.Streams.AppendToStreamAsync(
			streamName,
			expectedRevision,
			new[] {
				new EventData(Uuid.NewUuid(), "completed", Array.Empty<byte>(), contentType: "application/octet-stream")
			}
		);
	}

	void SubscriptionDropped(
		StreamSubscription _, SubscriptionDroppedReason __, Exception? ex,
		Func<Task<StreamSubscription>> resubscribe
	) {
		if (ex == null)
			return;

		if (ex.Message.Contains("too slow") && ex.Message.Contains("resubscription required")) {
			resubscribe();
			return;
		}

		Completed.TrySetException(ex);
	}

	Task EventAppeared(ResolvedEvent e, string streamName, out FromStream startFrom) {
		startFrom = FromStream.After(e.OriginalEventNumber);
		return EventAppeared(e, streamName);
	}

	Task EventAppeared(ResolvedEvent e, string streamName, out FromAll startFrom) {
		startFrom = FromAll.After(e.OriginalPosition!.Value);
		return EventAppeared(e, streamName);
	}

	Task EventAppeared(ResolvedEvent e, string streamName) {
		if (e.OriginalStreamId != streamName)
			return Task.CompletedTask;

		if (Seen[e.Event.EventNumber])
			throw new($"Event {e.Event.EventNumber} was already seen");

		Seen[e.Event.EventNumber] = true;
		if (e.Event.EventType == "completed")
			Completed.TrySetResult(true);

		return Task.CompletedTask;
	}
}
