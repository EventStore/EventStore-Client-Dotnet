using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace writing_events {
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

			// //append-to-stream
			// var eventData = new EventData(
			// 	Uuid.NewUuid(),
			// 	"some-event",
			// 	Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			// );
			//
			// await client.AppendToStreamAsync(
			// 	"some-stream",
			// 	StreamState.NoStream,
			// 	new List<EventData> {
			// 		eventData
			// 	});
			// //append-to-stream

			await AppendWithSameId(client);
		}

		private static async Task AppendWithSameId(EventStoreClient client) {
			//append-duplicate-event
			var eventData = new EventData(
				Guid.NewGuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			);
			
			await client.AppendToStreamAsync(
				"same-event-stream",
				StreamState.Any,
				new List<EventData> {
					eventData
				});
			
			// attempt to append the same event again
			await client.AppendToStreamAsync(
				"same-event-stream",
				StreamState.Any,
				new List<EventData> {
					eventData
				});
			//append-duplicate-event
		}
	}
}
