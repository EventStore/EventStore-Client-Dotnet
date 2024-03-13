using System.Net;

namespace EventStore.Client {
	internal class ChannelIdentifier(DnsEndPoint dnsEndpoint, UserCredentials? userCredentials = null) {
		public DnsEndPoint DnsEndpoint { get; } = dnsEndpoint;

		public UserCredentials? UserCredentials { get; } = userCredentials;
	}
}
