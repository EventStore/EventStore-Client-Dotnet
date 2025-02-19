namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when there is an invalid host in the KurrentDB connection string.
	/// </summary>
	public class InvalidHostException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="InvalidHostException"/>.
		/// </summary>
		/// <param name="host"></param>
		public InvalidHostException(string host)
			: base($"Invalid host: '{host}'") { }
	}
}
