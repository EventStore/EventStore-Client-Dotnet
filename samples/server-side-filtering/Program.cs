#pragma warning disable CS8321 // Local function is declared but never used

using EventTypeFilter = EventStore.Client.EventTypeFilter;

const int eventCount = 100;

var semaphore = new SemaphoreSlim(eventCount);

await using var client = new EventStoreClient(EventStoreClientSettings.Create("esdb://localhost:2113?tls=false"));

_ = Task.Run(async () => {
	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		filterOptions: new SubscriptionFilterOptions(EventTypeFilter.Prefix("some-")));
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				semaphore.Release();
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
});


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

	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}

	#endregion exclude-system
}

static async Task EventTypePrefix(EventStoreClient client) {
	#region event-type-prefix

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.Prefix("customer-"));

	#endregion event-type-prefix

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task EventTypeRegex(EventStoreClient client) {
	#region event-type-regex

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.RegularExpression("^user|^company"));

	#endregion event-type-regex

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task StreamPrefix(EventStoreClient client) {
	#region stream-prefix

	var filterOptions = new SubscriptionFilterOptions(StreamFilter.Prefix("user-"));

	#endregion stream-prefix

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task StreamRegex(EventStoreClient client) {
	#region stream-regex

	var filterOptions = new SubscriptionFilterOptions(StreamFilter.RegularExpression("^account|^savings"));

	#endregion stream-regex

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task CheckpointCallback(EventStoreClient client) {
	#region checkpoint

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents());

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
	
	#endregion checkpoint
}

static async Task CheckpointCallbackWithInterval(EventStoreClient client) {
	#region checkpoint-with-interval

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents(), 1000);

	#endregion checkpoint-with-interval

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
}
