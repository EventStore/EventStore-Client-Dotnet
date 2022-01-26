using System;
using System.Net;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when an operation requiring a leader node is made on a follower node.
	/// </summary>
	public class NotLeaderException : Exception {

		/// <summary>
		/// The <see cref="EndPoint"/> of the current leader node.
		/// </summary>
		public DnsEndPoint LeaderEndpoint { get; }

		/// <summary>
		/// Constructs a new <see cref="NotLeaderException"/>
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="exception"></param>
		public NotLeaderException(string host, int port, Exception? exception = null) : base(
			$"Not leader. New leader at {host}:{port}.", exception) {
			LeaderEndpoint = new DnsEndPoint(host, port);
		}
	}
}
