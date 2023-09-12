using System;

namespace EventStore.Client {
	/// <summary>
	/// Exception thrown when a user is not authorised to carry out
	/// an operation.
	/// </summary>
	public class InvalidPositionException : Exception {
		/// <summary>
		/// Constructs a new <see cref="InvalidPositionException" />.
		/// </summary>
		public InvalidPositionException(string message, Exception innerException) : base(message, innerException) {
		}

		/// <summary>
		/// Constructs a new <see cref="InvalidPositionException" />.
		/// </summary>
		public InvalidPositionException() : base("The specified position is invalid") {

		}
	}
}
