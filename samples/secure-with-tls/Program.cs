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
			var address = new Uri(Environment.GetEnvironmentVariable("ESDB_ADDRESS") ?? "https://localhost:2113");

			Console.WriteLine($"Connecting to EventStoreDB at: {address}");

			// setup settings
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = address
				}
			};

			using var client = new EventStoreClient(settings);
			
			var eventData = new EventData(
				Uuid.NewUuid(),
				"some-event",
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}")
			);


			try {
				var appendResult = await client.AppendToStreamAsync(
					"some-stream",
					StreamState.Any,
					new List<EventData> {
						eventData
					});
				Console.WriteLine($"SUCCESS! Append result: {appendResult.LogPosition}");
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
