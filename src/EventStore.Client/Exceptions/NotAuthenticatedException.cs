using System;

namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when a user is not authenticated.
	/// </summary>
	public class NotAuthenticatedException : Exception {
		/// <summary>
		/// Constructs a new <see cref="NotAuthenticatedException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public NotAuthenticatedException(string message, Exception? innerException = null) : base(message, innerException) {
		}
	}
}
