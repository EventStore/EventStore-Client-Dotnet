using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when <see cref="System.Net.EndPoint"/> discovery fails.
	/// </summary>
	public class DiscoveryException : Exception {
		/// <summary>
		/// The configured number of discovery attempts.
		/// </summary>
		public int MaxDiscoverAttempts { get; }

		/// <summary>
		/// Constructs a new <see cref="DiscoveryException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		[Obsolete]
		public DiscoveryException(string message, Exception? innerException = null)
			: base(message, innerException) {
			MaxDiscoverAttempts = 0;
		}

		/// <summary>
		/// Constructs a new <see cref="DiscoveryException"/>.
		/// </summary>
		/// <param name="maxDiscoverAttempts">The configured number of discovery attempts.</param>
		public DiscoveryException(int maxDiscoverAttempts) : base(
			$"Failed to discover candidate in {maxDiscoverAttempts} attempts.") {
			MaxDiscoverAttempts = maxDiscoverAttempts;
		}
	}
}
