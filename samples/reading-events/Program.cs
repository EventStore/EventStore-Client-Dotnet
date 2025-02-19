#pragma warning disable CS8321 // Local function is declared but never used

await using var client = new KurrentClient(KurrentClientSettings.Create("esdb://localhost:2113?tls=false"));

var events = Enumerable.Range(0, 20)
	.Select(
		r => new EventData(
			Uuid.NewUuid(),
			"some-event",
			Encoding.UTF8.GetBytes($"{{\"id\": \"{r}\" \"value\": \"some value\"}}")
		)
	);

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	events
);

await ReadFromStream(client);

return;

static async Task ReadFromStream(KurrentClient client) {
	#region read-from-stream

	var events = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		StreamPosition.Start
	);

	#endregion read-from-stream

	#region iterate-stream

	await foreach (var @event in events) Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));

	#endregion iterate-stream

	#region #read-from-stream-positions

	Console.WriteLine(events.FirstStreamPosition);
	Console.WriteLine(events.LastStreamPosition);

	#endregion
}

static async Task ReadFromStreamMessages(KurrentClient client) {
	#region read-from-stream-messages

	var streamPosition = StreamPosition.Start;
	var results = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		streamPosition
	);

	#endregion read-from-stream-messages

	#region iterate-stream-messages

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Ok ok:
				Console.WriteLine("Stream found.");
				break;

			case StreamMessage.NotFound:
				Console.WriteLine("Stream not found.");
				return;

			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.FirstStreamPosition(var sp):
				Console.WriteLine($"{sp} is after {streamPosition}; updating checkpoint.");
				streamPosition = sp;
				break;

			case StreamMessage.LastStreamPosition(var sp):
				Console.WriteLine($"The end of the stream is {sp}");
				break;

			default:
				break;
		}

	#endregion iterate-stream-messages
}

static async Task ReadFromStreamPosition(KurrentClient client) {
	#region read-from-stream-position

	var events = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		10,
		20
	);

	#endregion read-from-stream-position

	#region iterate-stream

	await foreach (var @event in events) Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));

	#endregion iterate-stream
}

static async Task ReadFromStreamPositionCheck(KurrentClient client) {
	#region checking-for-stream-presence

	var result = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		10,
		20
	);

	if (await result.ReadState == ReadState.StreamNotFound) return;

	await foreach (var e in result) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion checking-for-stream-presence
}

static async Task ReadFromStreamBackwards(KurrentClient client) {
	#region reading-backwards

	var events = client.ReadStreamAsync(
		Direction.Backwards,
		"some-stream",
		StreamPosition.End
	);

	await foreach (var e in events) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion reading-backwards
}

static async Task ReadFromStreamMessagesBackwards(KurrentClient client) {
	#region read-from-stream-messages-backwards

	var results = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		StreamPosition.End
	);

	#endregion read-from-stream-messages-backwards

	#region iterate-stream-messages-backwards

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Ok ok:
				Console.WriteLine("Stream found.");
				break;

			case StreamMessage.NotFound:
				Console.WriteLine("Stream not found.");
				return;

			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastStreamPosition(var sp):
				Console.WriteLine($"The end of the stream is {sp}");
				break;
		}

	#endregion iterate-stream-messages-backwards
}

static async Task ReadFromAllStream(KurrentClient client) {
	#region read-from-all-stream

	var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

	#endregion read-from-all-stream

	#region read-from-all-stream-iterate

	await foreach (var e in events) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion read-from-all-stream-iterate
}

static async Task ReadFromAllStreamMessages(KurrentClient client) {
	#region read-from-all-stream-messages

	var position = Position.Start;
	var results = client.ReadAllAsync(
		Direction.Forwards,
		position
	);

	#endregion read-from-all-stream-messages

	#region iterate-all-stream-messages

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastAllStreamPosition(var p):
				Console.WriteLine($"The end of the $all stream is {p}");
				break;
		}

	#endregion iterate-all-stream-messages
}

static async Task IgnoreSystemEvents(KurrentClient client) {
	#region ignore-system-events

	var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

	await foreach (var e in events) {
		if (e.Event.EventType.StartsWith("$")) continue;

		Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
	}

	#endregion ignore-system-events
}

static async Task ReadFromAllStreamBackwards(KurrentClient client) {
	#region read-from-all-stream-backwards

	var events = client.ReadAllAsync(Direction.Backwards, Position.End);

	#endregion read-from-all-stream-backwards

	#region read-from-all-stream-iterate

	await foreach (var e in events) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion read-from-all-stream-iterate
}

static async Task ReadFromAllStreamBackwardsMessages(KurrentClient client) {
	#region read-from-all-stream-messages-backwards

	var position = Position.End;
	var results = client.ReadAllAsync(
		Direction.Backwards,
		position
	);

	#endregion read-from-all-stream-messages-backwards

	#region iterate-all-stream-messages-backwards

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastAllStreamPosition(var p):
				Console.WriteLine($"{p} is before {position}; updating checkpoint.");
				position = p;
				break;
		}

	#endregion iterate-all-stream-messages-backwards
}

static async Task FilteringOutSystemEvents(KurrentClient client) {
	var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

	await foreach (var e in events)
		if (!e.Event.EventType.StartsWith("$"))
			Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
}

static void ReadStreamOverridingUserCredentials(KurrentClient client, CancellationToken cancellationToken) {
	#region overriding-user-credentials

	var result = client.ReadStreamAsync(
		Direction.Forwards,
		"some-stream",
		StreamPosition.Start,
		userCredentials: new UserCredentials("admin", "changeit"),
		cancellationToken: cancellationToken
	);

	#endregion overriding-user-credentials
}

static void ReadAllOverridingUserCredentials(KurrentClient client, CancellationToken cancellationToken) {
	#region read-all-overriding-user-credentials

	var result = client.ReadAllAsync(
		Direction.Forwards,
		Position.Start,
		userCredentials: new UserCredentials("admin", "changeit"),
		cancellationToken: cancellationToken
	);

	#endregion read-all-overriding-user-credentials
}

static void ReadAllResolvingLinkTos(KurrentClient client, CancellationToken cancellationToken) {
	#region read-from-all-stream-resolving-link-Tos

	var result = client.ReadAllAsync(
		Direction.Forwards,
		Position.Start,
		resolveLinkTos: true,
		cancellationToken: cancellationToken
	);

	#endregion read-from-all-stream-resolving-link-Tos
}
