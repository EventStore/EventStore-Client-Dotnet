using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when <see cref="System.Net.EndPoint"/> discovery fails.
	/// </summary>
	public class DiscoveryException : Exception {
		/// <summary>
		/// Constructs a new <see cref="DiscoveryException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public DiscoveryException(string message, Exception? innerException = null)
			: base(message, innerException) {
		}
	}
}
