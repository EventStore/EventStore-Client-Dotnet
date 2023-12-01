#pragma warning disable CS8321 // Local function is declared but never used

using EventTypeFilter = EventStore.Client.EventTypeFilter;

const int eventCount = 100;

var semaphore = new SemaphoreSlim(eventCount);

await using var client = new EventStoreClient(EventStoreClientSettings.Create("esdb://localhost:2113?tls=false"));

await client.SubscribeToAllAsync(
	FromAll.Start,
	(s, e, c) => {
		Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
		semaphore.Release();
		return Task.CompletedTask;
	},
	filterOptions: new SubscriptionFilterOptions(
		EventTypeFilter.Prefix("some-"),
		1,
		(s, p, c) => {
			Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
			return Task.CompletedTask;
		}
	)
);

await Task.Delay(2000);

for (var i = 0; i < eventCount; i++) {
	var eventData = new EventData(
		Uuid.NewUuid(),
		i % 2 == 0 ? "some-event" : "other-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		Guid.NewGuid().ToString("N"),
		StreamRevision.None,
		new List<EventData> { eventData }
	);
}

await semaphore.WaitAsync();

return;

static async Task ExcludeSystemEvents(EventStoreClient client) {
	#region exclude-system

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents())
	);

	#endregion exclude-system
}

static async Task EventTypePrefix(EventStoreClient client) {
	#region event-type-prefix

	var filter = new SubscriptionFilterOptions(EventTypeFilter.Prefix("customer-"));

	#endregion event-type-prefix

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}

static async Task EventTypeRegex(EventStoreClient client) {
	#region event-type-regex

	var filter = new SubscriptionFilterOptions(EventTypeFilter.RegularExpression("^user|^company"));

	#endregion event-type-regex

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}

static async Task StreamPrefix(EventStoreClient client) {
	#region stream-prefix

	var filter = new SubscriptionFilterOptions(StreamFilter.Prefix("user-"));

	#endregion stream-prefix

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}

static async Task StreamRegex(EventStoreClient client) {
	#region stream-regex

	var filter = new SubscriptionFilterOptions(StreamFilter.RegularExpression("^account|^savings"));

	#endregion stream-regex

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}

static async Task CheckpointCallback(EventStoreClient client) {
	#region checkpoint

	var filter = new SubscriptionFilterOptions(
		EventTypeFilter.ExcludeSystemEvents(),
		checkpointReached: (s, p, c) => {
			Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
			return Task.CompletedTask;
		}
	);

	#endregion checkpoint

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}

static async Task CheckpointCallbackWithInterval(EventStoreClient client) {
	#region checkpoint-with-interval

	var filter = new SubscriptionFilterOptions(
		EventTypeFilter.ExcludeSystemEvents(),
		1000,
		(s, p, c) => {
			Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
			return Task.CompletedTask;
		}
	);

	#endregion checkpoint-with-interval

	await client.SubscribeToAllAsync(
		FromAll.Start,
		(s, e, c) => {
			Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
			return Task.CompletedTask;
		},
		filterOptions: filter
	);
}