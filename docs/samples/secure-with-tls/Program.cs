using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	class Program {
		static async Task Main(string[] args) {
			//creating-connection
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};

			var client = new EventStoreClient(settings);
			
			//append-to-stream
			var eventData = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			);

			await client.AppendToStreamAsync(
				"some-stream",
				StreamState.NoStream,
				new List<EventData> {
					eventData
				});
			//append-to-stream
		}
	}
}
