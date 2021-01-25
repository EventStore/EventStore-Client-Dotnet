using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	class Program {
		static async Task Main(string[] args) {
			var address = Environment.GetEnvironmentVariable("ESDB_ADDRESS") ?? "https://localhost:2113";
			
			Console.WriteLine($"Connecting to EventStoreDB at: {address}");
			
			//creating-connection
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = new Uri(address)
				}
			};

			var client = new EventStoreClient(settings);

			//append-to-stream
			var eventData = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			);

			var appendResult = await client.AppendToStreamAsync(
				"some-stream",
				StreamState.Any,
				new List<EventData> {
					eventData
				});
			//append-to-stream

			Console.WriteLine($"Append result: {appendResult.LogPosition}");
		}
	}
}
