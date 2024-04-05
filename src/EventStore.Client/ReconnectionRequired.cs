using System.Net;

namespace EventStore.Client;

abstract record ReconnectionRequired {
	public record None : ReconnectionRequired {
		public static readonly None Instance = new();
	}

	public record Rediscover : ReconnectionRequired {
		public static readonly Rediscover Instance = new();
	}

	public record NewLeader(DnsEndPoint EndPoint) : ReconnectionRequired;
}