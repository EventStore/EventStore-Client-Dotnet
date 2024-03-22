using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client {
	internal class ChannelIdentifier(DnsEndPoint dnsEndpoint, X509Certificate2? userCertificate = null) {
		public DnsEndPoint DnsEndpoint { get; } = dnsEndpoint;

		public X509Certificate2? UserCertificate { get; } = userCertificate;
	}
}
