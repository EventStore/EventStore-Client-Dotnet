using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	public class EventStoreClientConnectivitySettings {
		public Uri Address { get; set; } = new UriBuilder {
			Scheme = Uri.UriSchemeHttps,
			Port = 2113
		}.Uri;
		public int MaxDiscoverAttempts { get; set; }

		public EndPoint[] GossipSeeds =>
			((object?) DnsGossipSeeds ?? IpGossipSeeds) switch {
				DnsEndPoint[] dns => Array.ConvertAll<DnsEndPoint, EndPoint>(dns, x => x),
				IPEndPoint[] ip => Array.ConvertAll<IPEndPoint, EndPoint>(ip, x => x),
				_ => Array.Empty<EndPoint>()
			};

		public DnsEndPoint[]? DnsGossipSeeds { get; set; }
		public IPEndPoint[]? IpGossipSeeds { get; set; }
		public TimeSpan GossipTimeout { get; set; }
		public bool GossipOverHttps { get; set; } = true;
		public TimeSpan DiscoveryInterval { get; set; }
		public NodePreference NodePreference { get; set; }

		public static EventStoreClientConnectivitySettings Default => new EventStoreClientConnectivitySettings {
			MaxDiscoverAttempts = 10,
			GossipTimeout = TimeSpan.FromSeconds(5),
			GossipOverHttps = true,
			DiscoveryInterval = TimeSpan.FromMilliseconds(100),
			NodePreference = NodePreference.Leader,
		};
	}
}
