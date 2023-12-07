await using var client = new EventStorePersistentSubscriptionsClient(
	EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=false")
);

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
			Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
		}
	);

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

	#endregion create-persistent-subscription-to-all
}

static async Task ConnectToPersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
	#region subscribe-to-persistent-subscription-to-all

	await client.SubscribeToAllAsync(
		"subscription-group",
		async (subscription, evnt, retryCount, cancellationToken) => { await HandleEvent(evnt); },
		(subscription, dropReason, exception) => {
			Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
		}
	);

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
			Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
		}
	);

	#endregion subscribe-to-persistent-subscription-with-manual-acks
}

static async Task UpdatePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
	#region update-persistent-subscription

	var userCredentials = new UserCredentials("admin", "changeit");
	var settings = new PersistentSubscriptionSettings(
		true,
		checkPointLowerBound: 20
	);

	await client.UpdateToStreamAsync(
		"test-stream",
		"subscription-group",
		settings,
		userCredentials: userCredentials
	);

	#endregion update-persistent-subscription
}

static async Task DeletePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
	#region delete-persistent-subscription

	var userCredentials = new UserCredentials("admin", "changeit");
	await client.DeleteToStreamAsync(
		"test-stream",
		"subscription-group",
		userCredentials: userCredentials
	);

	#endregion delete-persistent-subscription
}

static async Task GetPersistentSubscriptionToStreamInfo(EventStorePersistentSubscriptionsClient client) {
	#region get-persistent-subscription-to-stream-info

	var userCredentials = new UserCredentials("admin", "changeit");
	var info = await client.GetInfoToStreamAsync(
		"test-stream",
		"subscription-group",
		userCredentials: userCredentials
	);

	Console.WriteLine($"GroupName: {info.GroupName}, EventSource: {info.EventSource}, Status: {info.Status}");

	#endregion get-persistent-subscription-to-stream-info
}

static async Task GetPersistentSubscriptionToAllInfo(EventStorePersistentSubscriptionsClient client) {
	#region get-persistent-subscription-to-all-info

	var userCredentials = new UserCredentials("admin", "changeit");
	var info = await client.GetInfoToAllAsync(
		"subscription-group",
		userCredentials: userCredentials
	);

	Console.WriteLine($"GroupName: {info.GroupName}, EventSource: {info.EventSource}, Status: {info.Status}");

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

	#endregion replay-parked-of-persistent-subscription-to-all
}

static async Task ListPersistentSubscriptionsToStream(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions-to-stream

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions = await client.ListToStreamAsync(
		"test-stream",
		userCredentials: userCredentials
	);

	foreach (var s in subscriptions)
		Console.WriteLine($"GroupName: {s.GroupName}, EventSource: {s.EventSource}, Status: {s.Status}");

	#endregion list-persistent-subscriptions-to-stream
}

static async Task ListPersistentSubscriptionsToAll(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions-to-all

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions   = await client.ListToAllAsync(userCredentials: userCredentials);

	foreach (var s in subscriptions)
		Console.WriteLine($"GroupName: {s.GroupName}, EventSource: {s.EventSource}, Status: {s.Status}");

	#endregion list-persistent-subscriptions-to-all
}

static async Task ListAllPersistentSubscriptions(EventStorePersistentSubscriptionsClient client) {
	#region list-persistent-subscriptions

	var userCredentials = new UserCredentials("admin", "changeit");
	var subscriptions   = await client.ListAllAsync(userCredentials: userCredentials);

	foreach (var s in subscriptions)
		Console.WriteLine($"GroupName: {s.GroupName}, EventSource: {s.EventSource}, Status: {s.Status}");

	#endregion list-persistent-subscriptions
}

static async Task RestartPersistentSubscriptionSubsystem(EventStorePersistentSubscriptionsClient client) {
	#region restart-persistent-subscription-subsystem

	var userCredentials = new UserCredentials("admin", "changeit");
	await client.RestartSubsystemAsync(userCredentials: userCredentials);

	#endregion restart-persistent-subscription-subsystem
}

static Task HandleEvent(ResolvedEvent evnt) => Task.CompletedTask;

class UnrecoverableException : Exception { }