using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client {
	/// <summary>
	/// A class used to describe how to connect to an instance of EventStoreDB.
	/// </summary>
	public class EventStoreClientConnectivitySettings {
		private const int  DefaultPort = 2113;
		private       bool _insecure;
		private       Uri? _address;

		/// <summary>
		/// The <see cref="Uri"/> of the EventStoreDB. Use this when connecting to a single node.
		/// </summary>
		public Uri? Address {
			get => IsSingleNode ? _address : null;
			set => _address = value;
		}

		internal Uri ResolvedAddressOrDefault => Address ?? DefaultAddress;

		private Uri DefaultAddress =>
			new UriBuilder {
				Scheme = _insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps,
				Port   = DefaultPort
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
				IPEndPoint[] ip   => Array.ConvertAll<IPEndPoint, EndPoint>(ip, x => x),
				_                 => Array.Empty<EndPoint>()
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
		[Obsolete]
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
		/// The optional amount of time to wait after which a keepalive ping is sent on the transport.
		/// </summary>
		public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// The optional amount of time to wait after which a sent keepalive ping is considered timed out.
		/// </summary>
		public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// True if pointing to a single EventStoreDB node.
		/// </summary>
		public bool IsSingleNode => GossipSeeds.Length == 0;

		/// <summary>
		/// True if communicating over an insecure channel; otherwise false.
		/// </summary>
		public bool Insecure {
			get => IsSingleNode ? string.Equals(Address?.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) : _insecure;
			set => _insecure = value;
		}

		/// <summary>
		/// True if certificates will be validated; otherwise false.
		/// </summary>
		public bool TlsVerifyCert { get; set; } = true;

		/// <summary>
		/// Path to a certificate file for secure connection. Not required for enabling secure connection. Useful for self-signed certificate
		/// that are not installed on the system trust store.
		/// </summary>
		public X509Certificate2? TlsCaFile { get; set; }

		/// <summary>
		/// Client certificate used for user authentication.
		/// </summary>
		public X509Certificate2? UserCertificate { get; set; }

		/// <summary>
		/// The default <see cref="EventStoreClientConnectivitySettings"/>.
		/// </summary>
		public static EventStoreClientConnectivitySettings Default => new EventStoreClientConnectivitySettings {
			MaxDiscoverAttempts = 10,
			GossipTimeout       = TimeSpan.FromSeconds(5),
			DiscoveryInterval   = TimeSpan.FromMilliseconds(100),
			NodePreference      = NodePreference.Leader,
			KeepAliveInterval   = TimeSpan.FromSeconds(10),
			KeepAliveTimeout    = TimeSpan.FromSeconds(10),
			TlsVerifyCert       = true,
		};
	}
}
