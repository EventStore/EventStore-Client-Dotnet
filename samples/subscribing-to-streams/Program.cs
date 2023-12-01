#pragma warning disable CS8321 // Local function is declared but never used

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

await using var client = new EventStoreClient(EventStoreClientSettings.Create("esdb://localhost:2113?tls=false"));

await SubscribeToStream(client);
await SubscribeToAll(client);
await OverridingUserCredentials(client);

return;

static async Task SubscribeToStream(EventStoreClient client) {
	#region subscribe-to-stream

	await client.SubscribeToStreamAsync(
		"some-stream",
		FromStream.Start,
		async (subscription, evnt, cancellationToken) => {
			Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
			await HandleEvent(evnt);
		}
	);

	#endregion subscribe-to-stream

	#region subscribe-to-stream-from-position

	await client.SubscribeToStreamAsync(
		"some-stream",
		FromStream.After(StreamPosition.FromInt64(20)),
		EventAppeared
	);

	#endregion subscribe-to-stream-from-position

	#region subscribe-to-stream-live

	await client.SubscribeToStreamAsync(
		"some-stream",
		FromStream.End,
		EventAppeared
	);

	#endregion subscribe-to-stream-live

	#region subscribe-to-stream-resolving-linktos

	await client.SubscribeToStreamAsync(
		"$et-myEventType",
		FromStream.Start,
		EventAppeared,
		true
	);

	#endregion subscribe-to-stream-resolving-linktos

	#region subscribe-to-stream-subscription-dropped

	var checkpoint = await ReadStreamCheckpointAsync();
	await client.SubscribeToStreamAsync(
		"some-stream",
		checkpoint is null ? FromStream.Start : FromStream.After(checkpoint.Value),
		async (subscription, evnt, cancellationToken) => {
			await HandleEvent(evnt);
			checkpoint = evnt.OriginalEventNumber;
		},
		subscriptionDropped: (subscription, reason, exception) => {
			Console.WriteLine($"Subscription was dropped due to {reason}. {exception}");
			if (reason != SubscriptionDroppedReason.Disposed)
				// Resubscribe if the client didn't stop the subscription
				ResubscribeToStream(checkpoint);
		}
	);

	#endregion subscribe-to-stream-subscription-dropped
}

static async Task SubscribeToAll(EventStoreClient client) {
	#region subscribe-to-all

	await client.SubscribeToAllAsync(
		FromAll.Start,
		async (subscription, evnt, cancellationToken) => {
			Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
			await HandleEvent(evnt);
		}
	);

	#endregion subscribe-to-all

	#region subscribe-to-all-from-position

	var result = await client.AppendToStreamAsync(
		"subscribe-to-all-from-position",
		StreamState.NoStream,
		new[] {
			new EventData(Uuid.NewUuid(), "-", ReadOnlyMemory<byte>.Empty)
		}
	);

	await client.SubscribeToAllAsync(
		FromAll.After(result.LogPosition),
		EventAppeared
	);

	#endregion subscribe-to-all-from-position

	#region subscribe-to-all-live

	await client.SubscribeToAllAsync(
		FromAll.End,
		EventAppeared
	);

	#endregion subscribe-to-all-live

	#region subscribe-to-all-subscription-dropped

	var checkpoint = await ReadCheckpointAsync();
	await client.SubscribeToAllAsync(
		checkpoint is null ? FromAll.Start : FromAll.After(checkpoint.Value),
		async (subscription, evnt, cancellationToken) => {
			await HandleEvent(evnt);
			checkpoint = evnt.OriginalPosition!.Value;
		},
		subscriptionDropped: (subscription, reason, exception) => {
			Console.WriteLine($"Subscription was dropped due to {reason}. {exception}");
			if (reason != SubscriptionDroppedReason.Disposed)
				// Resubscribe if the client didn't stop the subscription
				Resubscribe(checkpoint);
		}
	);

	#endregion subscribe-to-all-subscription-dropped
}

static async Task SubscribeToFiltered(EventStoreClient client) {
	#region stream-prefix-filtered-subscription

	var prefixStreamFilter = new SubscriptionFilterOptions(StreamFilter.Prefix("test-", "other-"));
	await client.SubscribeToAllAsync(
		FromAll.Start,
		EventAppeared,
		filterOptions: prefixStreamFilter
	);

	#endregion stream-prefix-filtered-subscription

	#region stream-regex-filtered-subscription

	var regexStreamFilter = StreamFilter.RegularExpression(@"/invoice-\d\d\d/g");

	#endregion stream-regex-filtered-subscription
}

static async Task OverridingUserCredentials(EventStoreClient client) {
	#region overriding-user-credentials

	await client.SubscribeToAllAsync(
		FromAll.Start,
		EventAppeared,
		userCredentials: new UserCredentials("admin", "changeit")
	);

	#endregion overriding-user-credentials
}

static Task EventAppeared(StreamSubscription subscription, ResolvedEvent evnt, CancellationToken cancellationToken) =>
	Task.CompletedTask;

static void SubscriptionDropped(StreamSubscription subscription, SubscriptionDroppedReason reason, Exception ex) { }

static Task HandleEvent(ResolvedEvent evnt) => Task.CompletedTask;

static void ResubscribeToStream(StreamPosition? checkpoint) { }

static void Resubscribe(Position? checkpoint) { }

static Task<StreamPosition?> ReadStreamCheckpointAsync() =>
	Task.FromResult(new StreamPosition?());

static Task<Position?> ReadCheckpointAsync() =>
	Task.FromResult(new Position?());