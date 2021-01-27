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
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://localhost:2113?Tls=false")
			);

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: new SubscriptionFilterOptions(
					EventTypeFilter.Prefix("some-"),
					1,
					(s, p, c) => {
						Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
						return Task.CompletedTask;
					})
			);

			Thread.Sleep(2000);

			for (var i = 0; i < 100; i++) {
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

		private static async Task ExcludeSystemEvents(EventStoreClient client) {
			#region exclude-system
			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: new SubscriptionFilterOptions(
					EventTypeFilter.ExcludeSystemEvents())
			);
			#endregion exclude-system
		}

		private static async Task EventTypePrefix(EventStoreClient client) {
			#region event-type-prefix
			var filter = new SubscriptionFilterOptions(
				EventTypeFilter.Prefix("customer-"));
			#endregion event-type-prefix

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}

		private static async Task EventTypeRegex(EventStoreClient client) {
			#region event-type-regex
			var filter = new SubscriptionFilterOptions(
				EventTypeFilter.RegularExpression("^user|^company"));
			#endregion event-type-regex

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}

		private static async Task StreamPrefix(EventStoreClient client) {
			#region stream-prefix
			var filter = new SubscriptionFilterOptions(
				StreamFilter.Prefix("user-"));
			#endregion stream-prefix

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}

		private static async Task StreamRegex(EventStoreClient client) {
			#region stream-regex
			var filter = new SubscriptionFilterOptions(
				StreamFilter.RegularExpression("^account|^savings"));
			#endregion stream-regex

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}

		private static async Task CheckpointCallback(EventStoreClient client) {
			#region checkpoint
			var filter = new SubscriptionFilterOptions(
				EventTypeFilter.ExcludeSystemEvents(),
				checkpointReached: (s, p, c) =>
				{
					Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
					return Task.CompletedTask;
				});
			#endregion checkpoint

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}

		private static async Task CheckpointCallbackWithInterval(EventStoreClient client) {
			#region checkpoint-with-interval
			var filter = new SubscriptionFilterOptions(
				EventTypeFilter.ExcludeSystemEvents(),
				checkpointInterval: 1000,
				checkpointReached: (s, p, c) =>
				{
					Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
					return Task.CompletedTask;
				});
			#endregion checkpoint-with-interval

			await client.SubscribeToAllAsync(Position.Start,
				(s, e, c) => {
					Console.WriteLine(
						$"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
					return Task.CompletedTask;
				},
				filterOptions: filter
			);
		}
	}
}
