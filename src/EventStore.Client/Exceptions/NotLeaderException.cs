using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	public class NotLeaderException : Exception {
		public EndPoint LeaderEndpoint { get; }

		public NotLeaderException(string host, int port, Exception? exception = null) : base(
			$"Not leader. New leader at {host}:{port}.", exception) {
			LeaderEndpoint = new DnsEndPoint(host, port);
		}
	}
}
