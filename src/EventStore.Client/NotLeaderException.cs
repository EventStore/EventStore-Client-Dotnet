using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	public class NotLeaderException : Exception {
		public EndPoint LeaderEndpoint { get; }

		public NotLeaderException(EndPoint newLeaderEndpoint, Exception? exception = null) : base(
			$"Not leader. New leader at {newLeaderEndpoint}.", exception) {
			LeaderEndpoint = newLeaderEndpoint;
		}
	}
}
