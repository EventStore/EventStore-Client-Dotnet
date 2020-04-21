using System;
using System.Net;
using EventStore.Client;

namespace connecting_to_a_cluster {
	class Program {
		static void Main(string[] args) {
		}

		private static void ConnectingToACluster() {
			#region connecting-to-a-cluster
			var settings = new EventStoreClientSettings {
				ConnectivitySettings =
				{
					DnsGossipSeeds = new[]
					{
						new DnsEndPoint("localhost", 1114),
						new DnsEndPoint("localhost", 2114),
						new DnsEndPoint("localhost", 3114),
					}
				}
			};

			var client = new EventStoreClient(settings);
			#endregion connecting-to-a-cluster
		}

		private static void ProvidingDefaultCredentials() {
			#region providing-default-credentials
			var settings = new EventStoreClientSettings {
				ConnectivitySettings =
				{
					DnsGossipSeeds = new[]
					{
						new DnsEndPoint("localhost", 1114),
						new DnsEndPoint("localhost", 2114),
						new DnsEndPoint("localhost", 3114),
					}
				},
				DefaultCredentials = new UserCredentials("admin", "changeit")
			};

			var client = new EventStoreClient(settings);
			#endregion providing-default-credentials
		}

		private static void ConnectingToAClusterComplex() {
			#region connecting-to-a-cluster-complex
			var settings = new EventStoreClientSettings {
				ConnectivitySettings =
				{
					DnsGossipSeeds = new[]
					{
						new DnsEndPoint("localhost", 1114),
						new DnsEndPoint("localhost", 2114),
						new DnsEndPoint("localhost", 3114),
					},
					DiscoveryInterval = TimeSpan.FromMilliseconds(30),
					GossipTimeout = TimeSpan.FromSeconds(10),
					NodePreference = NodePreference.Leader,
					MaxDiscoverAttempts = 5
				}
			};

			var client = new EventStoreClient(settings);
			#endregion connecting-to-a-cluster-complex
		}
	}
}
