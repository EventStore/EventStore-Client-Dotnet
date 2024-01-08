#pragma warning disable CS8321 // Local function is declared but never used

static void ConnectingToACluster() {
	#region connecting-to-a-cluster

	using var client = new EventStoreClient(
		EventStoreClientSettings.Create("esdb://localhost:1114,localhost:2114,localhost:3114")
	);

	#endregion connecting-to-a-cluster
}

static void ProvidingDefaultCredentials() {
	#region providing-default-credentials

	using var client = new EventStoreClient(
		EventStoreClientSettings.Create("esdb://admin:changeit@localhost:1114,localhost:2114,localhost:3114")
	);

	#endregion providing-default-credentials
}

static void ConnectingToAClusterComplex() {
	#region connecting-to-a-cluster-complex

	using var client = new EventStoreClient(
		EventStoreClientSettings.Create(
			"esdb://admin:changeit@localhost:1114,localhost:2114,localhost:3114?DiscoveryInterval=30000;GossipTimeout=10000;NodePreference=leader;MaxDiscoverAttempts=5"
		)
	);

	#endregion connecting-to-a-cluster-complex
}