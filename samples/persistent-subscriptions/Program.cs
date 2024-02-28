await using var client = new EventStorePersistentSubscriptionsClient(
	EventStoreClientSettings.Create("esdb://localhost:2113?tls=false&tlsVerifyCert=false")
);

await DeletePersistentSubscription(client);
await DeletePersistentSubscriptionToAll(client);

await CreatePersistentSubscription(client);
await UpdatePersistentSubscription(client);
await ConnectToPersistentSubscriptionToStream(client);
await CreatePersistentSubscriptionToAll(client);
await ConnectToPersistentSubscriptionToAll(client);
await ConnectToPersistentSubscriptionWithManualAcks(client);
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

static async Task ConnectToPersistentSubscriptionToStream(EventStorePersistentSubscriptionsClient client) {
	#region subscribe-to-persistent-subscription-to-stream

	var subscription = await client.SubscribeToStreamAsync(
		"test-stream",
		"subscription-group",
		async (subscription, evnt, retryCount, cancellationToken) => {
			await HandleEvent(evnt);
			await subscription.Ack(evnt);
		},
		(subscription, dropReason, exception) => {
			Console.WriteLine($"Subscription to stream was dropped due to {dropReason}. {exception?.Message}");
		}
	);

	Console.WriteLine("Subscription to stream started");
	#endregion subscribe-to-persistent-subscription-to-stream
}

static async Task CreatePersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
	#region create-persistent-subscription-to-all

	var userCredentials = new UserCredentials("admin", "changeit");
	var filter          = StreamFilter.Prefix("test");

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

static async Task ConnectToPersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
	#region subscribe-to-persistent-subscription-to-all

	await client.SubscribeToAllAsync(
		"subscription-group",
		async (subscription, evnt, retryCount, cancellationToken) => { await HandleEvent(evnt); },
		(subscription, dropReason, exception) => {
			Console.WriteLine($"Subscription to all was dropped due to {dropReason}. {exception?.Message}");
		}
	);

	Console.WriteLine("Subscription to all started");
	#endregion subscribe-to-persistent-subscription-to-all
}

static async Task ConnectToPersistentSubscriptionWithManualAcks(EventStorePersistentSubscriptionsClient client) {
	#region subscribe-to-persistent-subscription-with-manual-acks

	var subscription = await client.SubscribeToStreamAsync(
		"test-stream",
		"subscription-group",
		async (subscription, evnt, retryCount, cancellationToken) => {
			try {
				await HandleEvent(evnt);
				await subscription.Ack(evnt);
			}
			catch (UnrecoverableException ex) {
				await subscription.Nack(PersistentSubscriptionNakEventAction.Park, ex.Message, evnt);
			}
		},
		(subscription, dropReason, exception) => {
			Console.WriteLine($"Subscription to stream with manual acks was dropped due to {dropReason}. {exception?.Message}");
		}
	);

	Console.WriteLine("Subscription to stream with manual acks started");
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
	}
	catch (PersistentSubscriptionNotFoundException) {
		// ignore
	}
	catch (Exception ex)  {
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
	}
	catch (PersistentSubscriptionNotFoundException) {
		// ignore
	}
	catch (Exception ex) {
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
	var subscriptions   = await client.ListToAllAsync(userCredentials: userCredentials);

	var entries = subscriptions
		.Select(s => $"GroupName: {s.GroupName} EventSource: {s.EventSource} Status: {s.Status}");
	
	Console.WriteLine($"Subscriptions to all: [ {string.Join(", ", entries)} ]");
	
	#endregion list-persistent-subscriptions-to-all
}

static async Task ListAllPersistentSubscriptions(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions   = await client.ListAllAsync(userCredentials: userCredentials);

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

class UnrecoverableException : Exception { }