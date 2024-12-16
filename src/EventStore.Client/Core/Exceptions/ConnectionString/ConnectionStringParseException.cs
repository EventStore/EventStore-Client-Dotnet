using System;

namespace EventStore.Client {
	/// <summary>
	/// The base exception that is thrown when an EventStoreDB connection string could not be parsed.
	/// </summary>
	public class ConnectionStringParseException : Exception {
		/// <summary>
		/// Constructs a new <see cref="ConnectionStringParseException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
		public ConnectionStringParseException(string message, Exception? innerException = null) : base(message, innerException) { }
	}
}
