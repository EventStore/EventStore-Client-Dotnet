using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace reading_events {
	class Program {
		static async Task Main(string[] args) {
			var settings = new EventStoreClientSettings {
				CreateHttpMessageHandler = () =>
					new HttpClientHandler {
						ServerCertificateCustomValidationCallback =
							(message, certificate2, x509Chain, sslPolicyErrors) => true
					},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};

			var client = new EventStoreClient(settings);

			var events = Enumerable.Range(0, 20)
				.Select(r => new EventData(
					Uuid.NewUuid(),
					"some-event",
					Encoding.UTF8.GetBytes("{\"id\": \"" + r + "\" \"value\": \"some value\"}")));

			await client.AppendToStreamAsync(
				"some-stream",
				StreamState.Any,
				events);

			await ReadFromStream(client);
		}

		private static async Task ReadFromStream(EventStoreClient client) {
			#region read-from-stream
			var events = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				StreamPosition.Start);
			#endregion read-from-stream

			#region iterate-stream
			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
			}
			#endregion iterate-stream
		}

		private static async Task ReadFromStreamPosition(EventStoreClient client) {
			#region read-from-stream-position
			var events = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				revision: 10,
				maxCount: 20);
			#endregion read-from-stream-position

			#region iterate-stream
			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
			}
			#endregion iterate-stream
		}

		private static async Task ReadFromStreamBackwards(EventStoreClient client) {
			#region reading-backwards
			var events = client.ReadStreamAsync(
				Direction.Backwards,
				"some-stream",
				StreamPosition.End);

			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
			}
			#endregion reading-backwards
		}

		private static async Task ReadFromAllStream(EventStoreClient client) {
			var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
			}
		}

		private static async Task FilteringOutSystemEvents(EventStoreClient client) {
			var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

			await foreach (var @event in events) {
				if (!@event.Event.EventType.StartsWith("$")) {
					Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
				}
			}
		}

		private static async Task ReadFromStreamResolvingLinkTos(EventStoreClient client) {
			var events = client.ReadAllAsync(Direction.Forwards, Position.Start, resolveLinkTos: true);

			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
			}
		}
	}
}
