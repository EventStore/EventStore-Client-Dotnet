using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Grpc.Core;

namespace secure_with_tls {
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
//			var connectionString = "esdb://localhost:2111,localhost:2112,localhost:2113?tls=false"; // ?tls=true&tlsVerifyCert=false
			var connectionString = "esdb://localhost:2113?tls=false"; // ?tls=true&tlsVerifyCert=false

			Console.WriteLine($"Connecting to EventStoreDB at: `{connectionString}`");

			var settings = EventStoreClientSettings.Create(connectionString);
			settings.ConnectivitySettings.NodePreference = NodePreference.Leader;

			var followerSettings = EventStoreClientSettings.Create(connectionString);
			followerSettings.ConnectivitySettings.NodePreference = NodePreference.Follower;

			var readOnlyReplicaSettings = EventStoreClientSettings.Create(connectionString);
			readOnlyReplicaSettings.ConnectivitySettings.NodePreference = NodePreference.ReadOnlyReplica;

			//			settings.DefaultCredentials = new UserCredentials("admin", "changeit");

			await using var leaderClient = new EventStoreClient(settings);
			await using var followerClient = new EventStoreClient(followerSettings);
			await using var readOnlyReplicaClient = new EventStoreClient(readOnlyReplicaSettings);

			await using var operations = new EventStoreOperationsClient(settings);
			await using var persistentSubscriptions = new EventStorePersistentSubscriptionsClient(settings);


			try {


				// await new HeartbeatTester().Run(client, persistentSubscriptions);
				// await new DoStuff().Run(client);
				//await new DirtyReadTester().Run(client, operations);
				//await new SubscriptionStallTester().Run(client);
				await new AllSubscription().Run(leaderClient, followerClient, readOnlyReplicaClient);

			} catch (Exception exception) 
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
