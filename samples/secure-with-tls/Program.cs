﻿using Grpc.Core; 

// take the address from environment variable (when run with Docker) or use localhost by default 
var connectionString = Environment.GetEnvironmentVariable("ESDB__CONNECTION__STRING") 
                    ?? "esdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=false";

Console.WriteLine($"Connecting to EventStoreDB at: {connectionString}");

await using var client = new EventStoreClient(EventStoreClientSettings.Create(connectionString));

var eventData = new EventData(Uuid.NewUuid(), "some-event", "{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray());

try {
	var appendResult = await client.AppendToStreamAsync(
		"some-stream", StreamState.Any, new List<EventData> { eventData }
	);

	Console.WriteLine($"SUCCESS! Append result: {appendResult.LogPosition}");
}
catch (Exception exception) {
	const string noNodeConnectionErrorMessage = "No connection could be made because the target machine actively refused it.";
	const string connectionRefused = "Connection refused";
	const string certificateIsNotInstalledOrInvalidErrorMessage = "The remote certificate is invalid according to the validation procedure.";

	var innerException = exception.InnerException;
	
	if (innerException is RpcException rpcException) {
		if (rpcException.Message.Contains(noNodeConnectionErrorMessage)
		 || rpcException.Message.Contains(connectionRefused)) {
			Console.WriteLine(
				$"FAILED! {noNodeConnectionErrorMessage} "
			  + $"Please makes sure that: EventStoreDB node is running, you're using a valid IP "
			  + $"address or DNS name, that port is valid and exposed (forwarded) in node config."
			);

			return;
		}

		if (rpcException.Message.Contains(certificateIsNotInstalledOrInvalidErrorMessage)) {
			Console.WriteLine(
				$"FAILED! {certificateIsNotInstalledOrInvalidErrorMessage} "
			  + $"Please makes sure that you installed CA certificate on client environment "
			  + $"and that it was generated with IP address or DNS name used for connecting."
			);

			return;
		}
	}

	Console.WriteLine($"FAILED! {exception}");
}