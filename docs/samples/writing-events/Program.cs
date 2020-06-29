using System;
using System.Collections.Generic;
using System.Linq;
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

			//append-to-stream
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
			//append-to-stream

			await AppendWithConcurrencyCheck(client);
		}

		private static async Task AppendWithSameId(EventStoreClient client) {
			//append-duplicate-event
			var eventData = new EventData(
				Uuid.NewUuid(),
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

		private static async Task AppendWithNoStream(EventStoreClient client) {
			//append-with-no-stream
			var eventDataOne = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			);

			var eventDataTwo = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"2\" \"value\": \"some other value\"}")
			);

			await client.AppendToStreamAsync(
				"no-stream-stream",
				StreamState.NoStream,
				new List<EventData> {
					eventDataOne
				});

			// attempt to append the same event again
			await client.AppendToStreamAsync(
				"no-stream-stream",
				StreamState.NoStream,
				new List<EventData> {
					eventDataTwo
				});
			//append-with-no-stream
		}

		private static async Task AppendWithConcurrencyCheck(EventStoreClient client) {
			//append-with-concurrency-check
			var clientOneRead = client.ReadStreamAsync(
				Direction.Forwards,
				"concurrency-stream",
				StreamPosition.Start,
				configureOperationOptions: options => options.ThrowOnAppendFailure = false);
			var clientOneRevision = (await clientOneRead.LastAsync()).Event.EventNumber.ToUInt64();

			var clientTwoRead = client.ReadStreamAsync(Direction.Forwards, "concurrency-stream", StreamPosition.Start);
			var clientTwoRevision = (await clientTwoRead.LastAsync()).Event.EventNumber.ToUInt64();

			var clientOneData = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"clientOne\"}")
			);

			await client.AppendToStreamAsync(
				"no-stream-stream",
				clientOneRevision,
				new List<EventData> {
					clientOneData
				});

			var clientTwoData = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"2\" \"value\": \"clientTwo\"}")
			);

			await client.AppendToStreamAsync(
				"no-stream-stream",
				clientTwoRevision,
				new List<EventData> {
					clientTwoData
				});
			//append-with-concurrency-check
		}
	}
}
