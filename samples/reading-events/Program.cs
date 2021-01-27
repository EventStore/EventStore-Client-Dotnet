using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace reading_events {
	class Program {
		static async Task Main(string[] args) {
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?TlsVerifyCert=false")
			);

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

		private static async Task ReadFromStreamPositionCheck(EventStoreClient client) {
			#region checking-for-stream-presence
			var result = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				revision: 10,
				maxCount: 20);

			if (await result.ReadState == ReadState.StreamNotFound) {
				return;
			}

			await foreach (var e in result) {
				Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
			}
			#endregion checking-for-stream-presence
		}

		private static async Task ReadFromStreamBackwards(EventStoreClient client) {
			#region reading-backwards
			var events = client.ReadStreamAsync(
				Direction.Backwards,
				"some-stream",
				StreamPosition.End);

			await foreach (var e in events) {
				Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
			}
			#endregion reading-backwards
		}

		private static async Task ReadFromAllStream(EventStoreClient client) {
			#region read-from-all-stream
			var events = client.ReadAllAsync(
				Direction.Forwards, Position.Start);
			#endregion read-from-all-stream

			#region read-from-all-stream-iterate
			await foreach (var e in events) {
				Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
			}
			#endregion read-from-all-stream-iterate
		}

		private static async Task IgnoreSystemEvents(EventStoreClient client) {
			#region ignore-system-events
			var events = client.ReadAllAsync(
				Direction.Forwards, Position.Start);

			await foreach (var e in events) {
				if (e.Event.EventType.StartsWith("$")) {
					continue;
				}

				Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
			}
			#endregion ignore-system-events
		}

		private static async Task ReadFromAllStreamBackwards(EventStoreClient client) {
			#region read-from-all-stream-backwards
			var events = client.ReadAllAsync(
				Direction.Backwards, Position.End);
			#endregion read-from-all-stream-backwards

			#region read-from-all-stream-iterate
			await foreach (var e in events) {
				Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
			}
			#endregion read-from-all-stream-iterate
		}

		private static async Task FilteringOutSystemEvents(EventStoreClient client) {
			var events = client.ReadAllAsync(Direction.Forwards, Position.Start);

			await foreach (var e in events) {
				if (!e.Event.EventType.StartsWith("$")) {
					Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
				}
			}
		}

		private void ReadStreamOverridingUserCredentials(EventStoreClient client, CancellationToken cancellationToken)
		{
			#region overriding-user-credentials
			var result = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				StreamPosition.Start,
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: cancellationToken);
			#endregion overriding-user-credentials
		}

		private void ReadAllOverridingUserCredentials(EventStoreClient client, CancellationToken cancellationToken)
		{
			#region read-all-overriding-user-credentials
			var result = client.ReadAllAsync(
				Direction.Forwards,
				Position.Start,
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: cancellationToken);
			#endregion read-all-overriding-user-credentials
		}
        

		private void ReadAllResolvingLinkTos(EventStoreClient client, CancellationToken cancellationToken)
		{
			#region read-from-all-stream-resolving-link-Tos
			var result = client.ReadAllAsync(
				Direction.Forwards,
				Position.Start,
				resolveLinkTos: true,
				cancellationToken: cancellationToken);
			#endregion read-from-all-stream-resolving-link-Tos
		}
	}
}
