using System;

#nullable enable
namespace EventStore.Client {
	public class DiscoveryException : Exception {
		public DiscoveryException(string message, Exception? innerException = null)
			: base(message, innerException) {
		}
	}
}
