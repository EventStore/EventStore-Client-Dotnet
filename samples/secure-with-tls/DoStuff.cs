using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	class DoStuff {
		public async Task Run(EventStoreClient client) {
			// append some events
			var eventData1 = new EventData(Uuid.NewUuid(), "testtype", Encoding.UTF8.GetBytes("{\"id\": \"1\", \"value\": \"some value\"}"));
			var eventData2 = new EventData(Uuid.NewUuid(), "testtype", Encoding.UTF8.GetBytes("{\"id\": \"1\", \"value\": \"some value\"}"));
			var eventData3 = new EventData(Uuid.NewUuid(), "testtype", Encoding.UTF8.GetBytes("{\"id\": \"1\", \"value\": \"some value\"}"));

			var stream = $"test-{Guid.Empty}";
			var appendResult = await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				new List<EventData> { eventData1, eventData2, eventData3 });

			Console.WriteLine("Appended");



			await foreach (var e in client.ReadStreamAsync(Direction.Forwards, "$ce-test", StreamPosition.Start, resolveLinkTos: true, maxCount: 500)) {
				;
			}
			Console.WriteLine("Read");


			var earlyReturn = true;
			if (earlyReturn)
				return;

			await Task.Delay(500);

			var creds = new UserCredentials("admin", "changeit");

			await foreach (var e in client.ReadStreamAsync(Direction.Forwards, "$ce-test", StreamPosition.Start, resolveLinkTos: true, userCredentials: creds)) {
				;
			}

			await foreach (var e in client.ReadAllAsync(Direction.Forwards, Position.Start, resolveLinkTos: true, userCredentials: creds)) {
				if (e.OriginalStreamId == "$ce-test") {
					int i = 0;
					i++;
				}
			}

			var sub = await client.SubscribeToAllAsync(
				FromAll.Start,
				async (s, e, ct) => {
					if (e.OriginalStreamId == "$ce-test") {
						int i = 0;
						i++;
					}
					await Task.Yield();
				},
				resolveLinkTos: true,
				userCredentials: creds);
		}
	}
}
