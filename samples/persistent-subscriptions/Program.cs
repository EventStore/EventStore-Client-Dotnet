using System;
using System.Threading.Tasks;
using EventStore.Client;

namespace persistent_subscriptions
{
    class Program
    {
	    static async Task Main(string[] args) {
		    await using var client = new EventStorePersistentSubscriptionsClient(
			    EventStoreClientSettings.Create("esdb://localhost:2113?tls=false")
		    );
		    await CreatePersistentSubscription(client);
		    await ConnectToPersistentSubscriptionToStream(client);
		    await CreatePersistentSubscriptionToAll(client);
		    await ConnectToPersistentSubscriptionToAll(client);
		    await ConnectToPersistentSubscriptionWithManualAcks(client);
		    await UpdatePersistentSubscription(client);
		    await DeletePersistentSubscription(client);
	    }

	    static async Task CreatePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
		    #region create-persistent-subscription-to-stream
		    var userCredentials = new UserCredentials("admin", "changeit");

		    var settings = new PersistentSubscriptionSettings();
		    await client.CreateAsync(
			    "test-stream",
			    "subscription-group",
			    settings,
			    userCredentials);
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
			    }, (subscription, dropReason, exception) => {
				    Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
			    });
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
			    userCredentials);
		    #endregion create-persistent-subscription-to-all
	    }

	    static async Task ConnectToPersistentSubscriptionToAll(EventStorePersistentSubscriptionsClient client) {
		    #region subscribe-to-persistent-subscription-to-all
		    await client.SubscribeToAllAsync(
			    "subscription-group",
			    async (subscription, evnt, retryCount, cancellationToken) => {
				    await HandleEvent(evnt);
			    }, (subscription, dropReason, exception) => {
				    Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
			    });
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
				    } catch (UnrecoverableException ex) {
					    await subscription.Nack(PersistentSubscriptionNakEventAction.Park, ex.Message, evnt);
				    }
			    }, (subscription, dropReason, exception) => {
				    Console.WriteLine($"Subscription was dropped due to {dropReason}. {exception}");
			    });
		    #endregion subscribe-to-persistent-subscription-with-manual-acks
	    }

	    static async Task UpdatePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
		    #region update-persistent-subscription
		    var userCredentials = new UserCredentials("admin", "changeit");
		    var settings = new PersistentSubscriptionSettings(
			    resolveLinkTos: true,
			    minCheckPointCount: 20);

		    await client.UpdateAsync(
			    "test-stream",
			    "subscription-group",
			    settings,
			    userCredentials);
		    #endregion update-persistent-subscription
	    }

	    static async Task DeletePersistentSubscription(EventStorePersistentSubscriptionsClient client) {
		    #region delete-persistent-subscription
		    var userCredentials = new UserCredentials("admin", "changeit");
		    await client.DeleteAsync(
			    "test-stream",
			    "subscription-group",
			    userCredentials);
		    #endregion delete-persistent-subscription
	    }

	    static Task HandleEvent(ResolvedEvent evnt) {
		    return Task.CompletedTask;
	    }

	    class UnrecoverableException : Exception {
	    }
    }
}
