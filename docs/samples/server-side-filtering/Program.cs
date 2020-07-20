using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventTypeFilter = EventStore.Client.EventTypeFilter;

namespace server_side_filtering {
	class Program {
		static async Task Main() {
			var settings = new EventStoreClientSettings {
				CreateHttpMessageHandler = () =>
					new HttpClientHandler {
						ServerCertificateCustomValidationCallback =
							(message, certificate2, x509Chain, sslPolicyErrors) => true
					},
				DefaultCredentials = new UserCredentials("admin", "changeit"),
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};

			var client = new EventStoreClient(settings);

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: new SubscriptionFilterOptions(
					EventTypeFilter.Prefix("some-"),
					2,
					(s, p, c) => {
						Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
						return Task.CompletedTask;
					})
			);

			for (var i = 0; i < 10; i++) {
				var eventData = new EventData(
					Uuid.NewUuid(),
					i % 2 == 0 ? "some-event" : "other-event",
					Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
				);

				await client.AppendToStreamAsync(
					Guid.NewGuid().ToString("N"),
					StreamRevision.None,
					new List<EventData> {eventData}
				);
			}

			Console.ReadLine();
		}
	}
}
