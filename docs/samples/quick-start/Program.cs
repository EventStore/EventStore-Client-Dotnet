using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace quick_start {
	class Program {
		static async Task Main(string[] args) {
			#region creating-connection
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
			#endregion creating-connection

			#region append-to-stream
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
			#endregion append-to-stream

			#region read-stream
			var events = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				StreamPosition.Start,
				1);

			await foreach (var @event in events) {
				Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.Span));
			}
			#endregion read-stream
		}
	}
}
