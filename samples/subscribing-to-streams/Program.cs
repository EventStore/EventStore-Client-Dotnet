using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Client.Streams;

namespace subscribing_to_streams {
	class Program {
		static async Task Main(string[] args) {
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?TlsVerifyCert=false")
			);

			await SubscribeToStream(client);
			await SubscribeToAll(client);
			await OverridingUserCredentials(client);
		}

		private static async Task SubscribeToStream(EventStoreClient client) {
			#region subscribe-to-stream
			await client.SubscribeToStreamAsync("some-stream",
				async (subscription, evnt, cancellationToken) => {
					Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
					await HandleEvent(evnt);
				});
			#endregion subscribe-to-stream
			
			#region subscribe-to-stream-from-position
			await client.SubscribeToStreamAsync(
				"some-stream",
				StreamPosition.FromInt64(20),
				EventAppeared);
			#endregion subscribe-to-stream-from-position
		
			#region subscribe-to-stream-live
			await client.SubscribeToStreamAsync(
				"some-stream",
				StreamPosition.End,
				EventAppeared);
			#endregion subscribe-to-stream-live
			
			#region subscribe-to-stream-resolving-linktos
			await client.SubscribeToStreamAsync(
				"$et-myEventType",
				StreamPosition.Start,
				EventAppeared,
				resolveLinkTos: true);
			#endregion subscribe-to-stream-resolving-linktos

			#region subscribe-to-stream-subscription-dropped
			var checkpoint = StreamPosition.Start;
			await client.SubscribeToStreamAsync(
				"some-stream",
				checkpoint,
				eventAppeared: async (subscription, evnt, cancellationToken) => {
					await HandleEvent(evnt);
					checkpoint = evnt.OriginalEventNumber;
				},
				subscriptionDropped: ((subscription, reason, exception) => {
					Console.WriteLine($"Subscription was dropped due to {reason}. {exception}");
					if (reason != SubscriptionDroppedReason.Disposed) {
						// Resubscribe if the client didn't stop the subscription
						Resubscribe(checkpoint);
					}
				}));
			#endregion subscribe-to-stream-subscription-dropped
		}

		private static async Task SubscribeToAll(EventStoreClient client) {
			#region subscribe-to-all
			await client.SubscribeToAllAsync(
				async (subscription, evnt, cancellationToken) => {
					Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
					await HandleEvent(evnt);
				});
			#endregion subscribe-to-all

			#region subscribe-to-all-from-position
			await client.SubscribeToAllAsync(
				new Position(1056, 1056),
				EventAppeared);
			#endregion subscribe-to-all-from-position

			#region subscribe-to-all-live
			await client.SubscribeToAllAsync(
				Position.End,
				EventAppeared);
			#endregion subscribe-to-all-live
			
			#region subscribe-to-all-subscription-dropped
			var checkpoint = Position.Start;
			await client.SubscribeToAllAsync(
				checkpoint,
				eventAppeared: async (subscription, evnt, cancellationToken) => {
					await HandleEvent(evnt);
					checkpoint = evnt.OriginalPosition.Value;
				},
				subscriptionDropped: ((subscription, reason, exception) => {
					Console.WriteLine($"Subscription was dropped due to {reason}. {exception}");
					if (reason != SubscriptionDroppedReason.Disposed) {
						// Resubscribe if the client didn't stop the subscription
						Resubscribe(checkpoint);
					}
				}));
			#endregion subscribe-to-all-subscription-dropped
		}

		private static async Task SubscribeToFiltered(EventStoreClient client) {
			#region stream-prefix-filtered-subscription
			var prefixStreamFilter = new SubscriptionFilterOptions(StreamFilter.Prefix("test-", "other-"));
			await client.SubscribeToAllAsync(
				EventAppeared,
				filterOptions: prefixStreamFilter);
			#endregion stream-prefix-filtered-subscription

			#region stream-regex-filtered-subscription
			var regexStreamFilter = StreamFilter.RegularExpression(@"/invoice-\d\d\d/g");
			#endregion stream-regex-filtered-subscription
		}

		private static async Task OverridingUserCredentials(EventStoreClient client) {
			#region overriding-user-credentials
			await client.SubscribeToAllAsync(
				EventAppeared,
				userCredentials: new UserCredentials("admin", "changeit"));
			#endregion overriding-user-credentials
		}

		private static Task EventAppeared(StreamSubscription subscription, ResolvedEvent evnt,
			CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}

		private static void SubscriptionDropped(StreamSubscription subscription, SubscriptionDroppedReason reason,
			Exception ex) {
		}
		private static Task HandleEvent(ResolvedEvent evnt) {
			return Task.CompletedTask;
		}

		private static void Resubscribe(StreamPosition checkpoint) { }
		private static void Resubscribe(Position checkpoint) { }
	}
}
