#pragma warning disable CS8321 // Local function is declared but never used

var settings = KurrentClientSettings.Create("esdb://localhost:2113?tls=false");

settings.OperationOptions.ThrowOnAppendFailure = false;

await using var client = new KurrentClient(settings);

await AppendToStream(client);
await AppendWithConcurrencyCheck(client);
await AppendWithNoStream(client);
await AppendWithSameId(client);

return;

static async Task AppendToStream(KurrentClient client) {
	#region append-to-stream

	var eventData = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"some-stream",
		StreamState.NoStream,
		new List<EventData> {
			eventData
		}
	);

	#endregion append-to-stream
}

static async Task AppendWithSameId(KurrentClient client) {
	#region append-duplicate-event

	var eventData = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		new List<EventData> {
			eventData
		}
	);

	// attempt to append the same event again
	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		new List<EventData> {
			eventData
		}
	);

	#endregion append-duplicate-event
}

static async Task AppendWithNoStream(KurrentClient client) {
	#region append-with-no-stream

	var eventDataOne = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	var eventDataTwo = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"2\" \"value\": \"some other value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		StreamState.NoStream,
		new List<EventData> {
			eventDataOne
		}
	);

	// attempt to append the same event again
	await client.AppendToStreamAsync(
		"no-stream-stream",
		StreamState.NoStream,
		new List<EventData> {
			eventDataTwo
		}
	);

	#endregion append-with-no-stream
}

static async Task AppendWithConcurrencyCheck(KurrentClient client) {
	await client.AppendToStreamAsync(
		"concurrency-stream",
		StreamRevision.None,
		new[] { new EventData(Uuid.NewUuid(), "-", ReadOnlyMemory<byte>.Empty) }
	);

	#region append-with-concurrency-check

	var clientOneRead = client.ReadStreamAsync(
		Direction.Forwards,
		"concurrency-stream",
		StreamPosition.Start
	);

	var clientOneRevision = (await clientOneRead.LastAsync()).Event.EventNumber.ToUInt64();

	var clientTwoRead     = client.ReadStreamAsync(Direction.Forwards, "concurrency-stream", StreamPosition.Start);
	var clientTwoRevision = (await clientTwoRead.LastAsync()).Event.EventNumber.ToUInt64();

	var clientOneData = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"1\" \"value\": \"clientOne\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		clientOneRevision,
		new List<EventData> {
			clientOneData
		}
	);

	var clientTwoData = new EventData(
		Uuid.NewUuid(),
		"some-event",
		"{\"id\": \"2\" \"value\": \"clientTwo\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		clientTwoRevision,
		new List<EventData> {
			clientTwoData
		}
	);

	#endregion append-with-concurrency-check
}

static async Task AppendOverridingUserCredentials(KurrentClient client, CancellationToken cancellationToken) {
	var eventData = new EventData(
		Uuid.NewUuid(),
		"TestEvent",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	#region overriding-user-credentials

	await client.AppendToStreamAsync(
		"some-stream",
		StreamState.Any,
		new[] { eventData },
		userCredentials: new UserCredentials("admin", "changeit"),
		cancellationToken: cancellationToken
	);

	#endregion overriding-user-credentials
}
