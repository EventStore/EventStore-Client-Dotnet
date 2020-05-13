using System;
using System.Net;

namespace EventStore.Client {
	static class EndPointExtensions {
		public static string GetHost(this EndPoint endpoint) =>
			endpoint switch {
				IPEndPoint ip => ip.Address.ToString(),
				DnsEndPoint dns => dns.Host,
				_ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint?.GetType(),
					"An invalid endpoint has been provided")
			};

		public static int GetPort(this EndPoint endpoint) =>
			endpoint switch {
				IPEndPoint ip => ip.Port,
				DnsEndPoint dns => dns.Port,
				_ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint?.GetType(),
					"An invalid endpoint has been provided")
			};
	}
}
