using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	class TombStoneCheck {
		EventData CreateEvent(int length = 0) {
			var data = new string('#', length);
			return new EventData(
				Uuid.NewUuid(),
				"testtype",
				Encoding.UTF8.GetBytes(@$"{{""data"": ""{data}""}}"));
		}

		public async Task Run(
			EventStoreClient client,
			EventStoreOperationsClient operations) {


			var stream = $"test-{Guid.NewGuid()}";
			Console.WriteLine($"Appending to {stream}");

			// append some events to a new stream
			var appendResult = await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Enumerable.Range(0, 3).Select(_ => CreateEvent()).ToArray());

			Console.WriteLine("Appended");


			await client.SetStreamMetadataAsync(stream, StreamState.Any, new(truncateBefore: 1));
			Console.WriteLine("Written metadata");

			Console.ReadKey();

			// hard delete the stream
			await client.TombstoneAsync(stream, StreamState.Any);
			Console.WriteLine("Tombstoned");

			// write enough to get it to the next chunk
			for (int i = 0; i < 300; i++) {
				await client.AppendToStreamAsync(
					"padding",
					StreamState.Any,
					Enumerable.Range(0, 1).Select(_ => CreateEvent(1_000_000)).ToArray());
			}

			Console.WriteLine("Written padding");

		}
	}
}
