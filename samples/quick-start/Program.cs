using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace quick_start {
	public class TestEvent {
		public string? EntityId      { get; set; }
		public string? ImportantData { get; set; }
	}
	
	class Program {
		static void Main(string[] args) {
		}

		static async Task Samples() {
			CancellationTokenSource tokenSource = new CancellationTokenSource();
			CancellationToken cancellationToken = tokenSource.Token;
			
			#region createClient
			var settings = EventStoreClientSettings
				.Create("{connectionString}");
			var client = new EventStoreClient(settings);
			#endregion createClient

			#region createEvent
			var evt = new TestEvent
			{
				EntityId = Guid.NewGuid().ToString("N"),
				ImportantData = "I wrote my first event!"
			};

			var eventData = new EventData(
				Uuid.NewUuid(),
				"TestEvent",
				JsonSerializer.SerializeToUtf8Bytes(evt)
			);
			#endregion createEvent

			#region appendEvents
			await client.AppendToStreamAsync(
				"some-stream",
				StreamState.Any,
				new[] { eventData },
				cancellationToken: cancellationToken
			);
			#endregion appendEvents
            
			#region overriding-user-credentials
			await client.AppendToStreamAsync(
				"some-stream",
				StreamState.Any,
				new[] { eventData },
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: cancellationToken
			);
			#endregion overriding-user-credentials

			#region readStream
			var result = client.ReadStreamAsync(
				Direction.Forwards,
				"some-stream",
				StreamPosition.Start,
				cancellationToken: cancellationToken);

			var events = await result.ToListAsync(cancellationToken);
			#endregion readStream
		} 
	}
}
