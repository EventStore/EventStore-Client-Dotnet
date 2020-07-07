using System;
using System.Net;

namespace EventStore.Client {
	internal static class EndPointExtensions {
		public static string HTTP_SCHEMA => Uri.UriSchemeHttp;
		public static string HTTPS_SCHEMA => Uri.UriSchemeHttps;

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

		public static string ToHttpUrl(this EndPoint endPoint, string schema, string rawUrl = null) =>
			endPoint switch {
				IPEndPoint ipEndPoint => CreateHttpUrl(schema, ipEndPoint.Address.ToString(), ipEndPoint.Port,
					rawUrl != null ? rawUrl.TrimStart('/') : string.Empty),
				DnsEndPoint dnsEndpoint => CreateHttpUrl(schema, dnsEndpoint.Host, dnsEndpoint.Port,
					rawUrl != null ? rawUrl.TrimStart('/') : string.Empty),
				_ => null
			};

		private static string CreateHttpUrl(string schema, string host, int port, string path) {
			return $"{schema}://{host}:{port}/{path}";
		}
	}
}
