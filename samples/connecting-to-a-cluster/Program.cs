using System;
using System.Net;
using EventStore.Client;

namespace connecting_to_a_cluster {
	class Program {
		static void Main(string[] args) {
		}

		private static void ConnectingToACluster() {
			#region connecting-to-a-cluster
			
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://localhost:1114,localhost:2114,localhost:3114")
			);
			#endregion connecting-to-a-cluster
		}

		private static void ProvidingDefaultCredentials() {
			#region providing-default-credentials
			
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://admin:changeit@localhost:1114,localhost:2114,localhost:3114")
			);
			#endregion providing-default-credentials
		}

		private static void ConnectingToAClusterComplex() {
			#region connecting-to-a-cluster-complex
			
			using var client = new EventStoreClient(
				EventStoreClientSettings.Create("esdb://admin:changeit@localhost:1114,localhost:2114,localhost:3114?DiscoveryInterval=30000;GossipTimeout=10000;NodePreference=leader;MaxDiscoverAttempts=5")
			);
			#endregion connecting-to-a-cluster-complex
		}
	}
}
