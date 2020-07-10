using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A class used to describe how to connect to an instance of EventStoreDB.
	/// </summary>
	public class EventStoreClientConnectivitySettings {
		/// <summary>
		/// The <see cref="Uri"/> of the EventStoreDB. Use this when connecting to a single node.
		/// </summary>
		public Uri Address { get; set; } = new UriBuilder {
			Scheme = Uri.UriSchemeHttps,
			Port = 2113
		}.Uri;

		/// <summary>
		/// The maximum number of times to attempt <see cref="EndPoint"/> discovery.
		/// </summary>
		public int MaxDiscoverAttempts { get; set; }

		/// <summary>
		/// An array of <see cref="EndPoint"/>s used to seed gossip.
		/// </summary>
		public EndPoint[] GossipSeeds =>
			((object?)DnsGossipSeeds ?? IpGossipSeeds) switch {
				DnsEndPoint[] dns => Array.ConvertAll<DnsEndPoint, EndPoint>(dns, x => x),
				IPEndPoint[] ip => Array.ConvertAll<IPEndPoint, EndPoint>(ip, x => x),
				_ => Array.Empty<EndPoint>()
			};

		/// <summary>
		/// An array of <see cref="DnsEndPoint"/>s to use for seeding gossip. This will be checked before <see cref="IpGossipSeeds"/>.
		/// </summary>
		public DnsEndPoint[]? DnsGossipSeeds { get; set; }

		/// <summary>
		/// An array of <see cref="IPEndPoint"/>s to use for seeding gossip. This will be checked after <see cref="DnsGossipSeeds"/>.
		/// </summary>
		public IPEndPoint[]? IpGossipSeeds { get; set; }

		/// <summary>
		/// The <see cref="TimeSpan"/> after which an attempt to discover gossip will fail.
		/// </summary>
		public TimeSpan GossipTimeout { get; set; }

		/// <summary>
		/// Whether or not to use HTTPS when communicating via gossip.
		/// </summary>
		public bool GossipOverHttps { get; set; } = true;

		/// <summary>
		/// The polling interval used to discover the <see cref="EndPoint"/>.
		/// </summary>
		public TimeSpan DiscoveryInterval { get; set; }

		/// <summary>
		/// The <see cref="NodePreference"/> to use when connecting.
		/// </summary>
		public NodePreference NodePreference { get; set; }


		/// <summary>
		/// True if pointing to a single EventStoreDB node.
		/// </summary>
		public bool IsSingleNode => GossipSeeds.Length == 0;

		/// <summary>
		/// The default <see cref="EventStoreClientConnectivitySettings"/>.
		/// </summary>
		public static EventStoreClientConnectivitySettings Default => new EventStoreClientConnectivitySettings {
			MaxDiscoverAttempts = 10,
			GossipTimeout = TimeSpan.FromSeconds(5),
			GossipOverHttps = true,
			DiscoveryInterval = TimeSpan.FromMilliseconds(100),
			NodePreference = NodePreference.Leader
		};
	}
}
