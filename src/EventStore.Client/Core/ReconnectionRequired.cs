using System.Net;

namespace EventStore.Client {
	internal abstract record ReconnectionRequired {
		public record None : ReconnectionRequired {
			public static None Instance = new();
		}

		public record Rediscover : ReconnectionRequired {
			public static Rediscover Instance = new();
		}

		public record NewLeader(DnsEndPoint EndPoint) : ReconnectionRequired;
	}
}
