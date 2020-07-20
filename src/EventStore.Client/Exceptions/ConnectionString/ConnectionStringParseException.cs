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
		public ConnectionStringParseException(string message) : base(message) { }
	}
}
