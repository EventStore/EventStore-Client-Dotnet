using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Grpc.Core;

namespace secure_with_tls 
{
	class Program 
	{
		private const string NoNodeConnectionErrorMessage =
			"No connection could be made because the target machine actively refused it.";
		
		private const string ConnectionRefused =
			"Connection refused";

		private const string CertificateIsNotInstalledOrInvalidErrorMessage =
			"The remote certificate is invalid according to the validation procedure.";
		
		static async Task Main(string[] args) 
		{
			// take the address from environment variable (when run with Docker) or use localhost by default 
			var connectionString = Environment.GetEnvironmentVariable("ESDB_CONNECTION_STRING") ?? "esdb://localhost:2113?tls=true&tlsVerifyCert=false";

			Console.WriteLine($"Connecting to EventStoreDB at: `{connectionString}`");

			var settings = EventStoreClientSettings.Create(connectionString);
			await using var client = new EventStoreClient(settings);

			//qq


			try {
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


				client.ReadStreamAsync(Direction.Backwards, stream, StreamP)

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
			catch (Exception exception) 
			{
				var innerException = exception.InnerException;

				if (innerException is RpcException rpcException) 
				{
					if (rpcException.Message.Contains(NoNodeConnectionErrorMessage) || rpcException.Message.Contains(ConnectionRefused)) {
						Console.WriteLine(
							$"FAILED! {NoNodeConnectionErrorMessage} Please makes sure that: EventStoreDB node is running, you're using a valid IP address or DNS name, that port is valid and exposed (forwarded) in node config.");
						return;
					}

					if (rpcException.Message.Contains(CertificateIsNotInstalledOrInvalidErrorMessage))
					{
						Console.WriteLine(
							$"FAILED! {CertificateIsNotInstalledOrInvalidErrorMessage} Please makes sure that you installed CA certificate on client environment and that it was generated with IP address or DNS name used for connecting.");
						return;
					}
				}
				Console.WriteLine($"FAILED! {exception}");
			}
		}
	}
}
