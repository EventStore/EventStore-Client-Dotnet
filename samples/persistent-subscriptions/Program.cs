await using var client = new EventStorePersistentSubscriptionsClient(
	EventStoreClientSettings.Create("esdb://localhost:2113?tls=false&tlsVerifyCert=false")
);

await DeletePersistentSubscription(client);
await DeletePersistentSubscriptionToAll(client);

await CreatePersistentSubscription(client);
await UpdatePersistentSubscription(client);

try {
	await ConnectToPersistentSubscriptionToStream(client, GetCT());
} catch (OperationCanceledException) { }

await CreatePersistentSubscriptionToAll(client);

try {
	await ConnectToPersistentSubscriptionToAll(client, GetCT());
} catch (OperationCanceledException) { }

try {
	await ConnectToPersistentSubscriptionWithManualAcks(client, GetCT());
} catch (OperationCanceledException) { }

await GetPersistentSubscriptionToStreamInfo(client);
await GetPersistentSubscriptionToAllInfo(client);
await ReplayParkedToStream(client);
await ReplayParkedToAll(client);
await ListPersistentSubscriptionsToStream(client);
await ListPersistentSubscriptionsToAll(client);
await ListAllPersistentSubscriptions(client);
await RestartPersistentSubscriptionSubsystem(client);

await DeletePersistentSubscription(client);
await DeletePersistentSubscriptionToAll(client);

return;

static async Task CreatePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
	#region create-persistent-subscription-to-stream

	var userCredentials = new UserCredentials("admin", "changeit");

	var settings = new PersistentSubscriptionSettings();
	await client.CreateToStreamAsync(
		"test-stream",
		"subscription-group",
		settings,
		userCredentials: userCredentials
	);

	Console.WriteLine("Subscription to stream created");

	#endregion create-persistent-subscription-to-stream
}

static async Task ConnectToPersistentSubscriptionToStream(EventStorePersistentSubscriptionsClient client,
	CancellationToken ct) {
	#region subscribe-to-persistent-subscription-to-stream

	await using var subscription = client.SubscribeToStream(
		"test-stream",
		"subscription-group", 
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId):
				Console.WriteLine($"Subscription {subscriptionId} to stream started");
				break;
			case PersistentSubscriptionMessage.Event(var resolvedEvent, _):
				await HandleEvent(resolvedEvent);
				await subscription.Ack(resolvedEvent);
				break;
		}
	}

	#endregion subscribe-to-persistent-subscription-to-stream
}

static async Task CreatePersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
	#region create-persistent-subscription-to-all

	var userCredentials = new UserCredentials("admin", "changeit");
	var filter = StreamFilter.Prefix("test");

	var settings = new PersistentSubscriptionSettings();
	await client.CreateToAllAsync(
		"subscription-group",
		filter,
		settings,
		userCredentials: userCredentials
	);

	Console.WriteLine("Subscription to all created");

	#endregion create-persistent-subscription-to-all
}

static async Task ConnectToPersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client,
	CancellationToken ct) {
	#region subscribe-to-persistent-subscription-to-all

	await using var subscription = client.SubscribeToAll(
		"subscription-group",
		cancellationToken: ct);

	await foreach (var message in subscription.Messages) {
		switch (message) {
			case PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId):
				Console.WriteLine($"Subscription {subscriptionId} to stream started");
				break;
			case PersistentSubscriptionMessage.Event(var resolvedEvent, _):
				await HandleEvent(resolvedEvent);
				break;
		}
	}

	#endregion subscribe-to-persistent-subscription-to-all
}

static async Task ConnectToPersistentSubscriptionWithManualAcks(EventStorePersistentSubscriptionsClient client,
	CancellationToken ct) {
	#region subscribe-to-persistent-subscription-with-manual-acks

	await using var subscription = client.SubscribeToStream(
		"test-stream",
		"subscription-group",
		cancellationToken: ct);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId):
				Console.WriteLine($"Subscription {subscriptionId} to stream with manual acks started");
				break;
			case PersistentSubscriptionMessage.Event(var resolvedEvent, _):
				try {
					await HandleEvent(resolvedEvent);
					await subscription.Ack(resolvedEvent);
				} catch (UnrecoverableException ex) {
					await subscription.Nack(PersistentSubscriptionNakEventAction.Park, ex.Message, resolvedEvent);
				}
				break;
		}
	}

	#endregion subscribe-to-persistent-subscription-with-manual-acks
}

static async Task UpdatePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
	#region update-persistent-subscription

	var userCredentials = new UserCredentials("admin", "changeit");
	var settings = new PersistentSubscriptionSettings(true, checkPointLowerBound: 20);

	await client.UpdateToStreamAsync(
		"test-stream",
		"subscription-group",
		settings,
		userCredentials: userCredentials
	);

	Console.WriteLine("Subscription updated");

	#endregion update-persistent-subscription
}

static async Task DeletePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
	#region delete-persistent-subscription

	try {
		var userCredentials = new UserCredentials("admin", "changeit");
		await client.DeleteToStreamAsync(
			"test-stream",
			"subscription-group",
			userCredentials: userCredentials
		);

		Console.WriteLine("Subscription to stream deleted");
	} catch (PersistentSubscriptionNotFoundException) {
		// ignore
	} catch (Exception ex) {
		Console.WriteLine($"Subscription to stream delete error: {ex.GetType()} {ex.Message}");
	}

	#endregion delete-persistent-subscription
}

static async Task DeletePersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
	#region delete-persistent-subscription-to-all

	try {
		var userCredentials = new UserCredentials("admin", "changeit");
		await client.DeleteToAllAsync(
			"subscription-group",
			userCredentials: userCredentials
		);

		Console.WriteLine("Subscription to all deleted");
	} catch (PersistentSubscriptionNotFoundException) {
		// ignore
	} catch (Exception ex) {
		Console.WriteLine($"Subscription to all delete error: {ex.GetType()} {ex.Message}");
	}

	#endregion delete-persistent-subscription-to-all
}

static async Task GetPersistentSubscriptionToStreamInfo(EventStorePersistentSubscriptionsClient client) {
	#region get-persistent-subscription-to-stream-info

	var userCredentials = new UserCredentials("admin", "changeit");
	var info = await client.GetInfoToStreamAsync(
		"test-stream",
		"subscription-group",
		userCredentials: userCredentials
	);

	Console.WriteLine($"GroupName: {info.GroupName} EventSource: {info.EventSource} Status: {info.Status}");

	#endregion get-persistent-subscription-to-stream-info
}

static async Task GetPersistentSubscriptionToAllInfo(EventStorePersistentSubscriptionsClient client) {
	#region get-persistent-subscription-to-all-info

	var userCredentials = new UserCredentials("admin", "changeit");
	var info = await client.GetInfoToAllAsync(
		"subscription-group",
		userCredentials: userCredentials
	);

	Console.WriteLine($"GroupName: {info.GroupName} EventSource: {info.EventSource} Status: {info.Status}");

	#endregion get-persistent-subscription-to-all-info
}

static async Task ReplayParkedToStream(EventStorePersistentSubscriptionsClient client) {
	#region replay-parked-of-persistent-subscription-to-stream

	var userCredentials = new UserCredentials("admin", "changeit");
	await client.ReplayParkedMessagesToStreamAsync(
		"test-stream",
		"subscription-group",
		10,
		userCredentials: userCredentials
	);

	Console.WriteLine("Replay of parked messages to stream requested");

	#endregion persistent-subscription-replay-parked-to-stream
}

static async Task ReplayParkedToAll(EventStorePersistentSubscriptionsClient client) {
	#region replay-parked-of-persistent-subscription-to-all

	var userCredentials = new UserCredentials("admin", "changeit");
	await client.ReplayParkedMessagesToAllAsync(
		"subscription-group",
		10,
		userCredentials: userCredentials
	);

	Console.WriteLine("Replay of parked messages to all requested");

	#endregion replay-parked-of-persistent-subscription-to-all
}

static async Task ListPersistentSubscriptionsToStream(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions-to-stream

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions = await client.ListToStreamAsync(
		"test-stream",
		userCredentials: userCredentials
	);

	var entries = subscriptions
		.Select(s => $"GroupName: {s.GroupName} EventSource: {s.EventSource} Status: {s.Status}");

	Console.WriteLine($"Subscriptions to stream: [ {string.Join(", ", entries)} ]");

	#endregion list-persistent-subscriptions-to-stream
}

static async Task ListPersistentSubscriptionsToAll(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions-to-all

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions = await client.ListToAllAsync(userCredentials: userCredentials);

	var entries = subscriptions
		.Select(s => $"GroupName: {s.GroupName} EventSource: {s.EventSource} Status: {s.Status}");

	Console.WriteLine($"Subscriptions to all: [ {string.Join(", ", entries)} ]");

	#endregion list-persistent-subscriptions-to-all
}

static async Task ListAllPersistentSubscriptions(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions = await client.ListAllAsync(userCredentials: userCredentials);

	var entries = subscriptions
		.Select(s => $"GroupName: {s.GroupName} EventSource: {s.EventSource} Status: {s.Status}");

	Console.WriteLine($"Subscriptions: [{string.Join(", ", entries)} ]");

	#endregion list-persistent-subscriptions
}

static async Task RestartPersistentSubscriptionSubsystem(EventStorePersistentSubscriptionsClient client) {
	#region restart-persistent-subscription-subsystem

	var userCredentials = new UserCredentials("admin", "changeit");
	await client.RestartSubsystemAsync(userCredentials: userCredentials);

	Console.WriteLine("Persistent subscription subsystem restarted");

	#endregion restart-persistent-subscription-subsystem
}

static Task HandleEvent(ResolvedEvent evnt) => Task.CompletedTask;

// ensures that samples exit in a timely manner on CI
static CancellationToken GetCT() => new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

class UnrecoverableException : Exception {
}
