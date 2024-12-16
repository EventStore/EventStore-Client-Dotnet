namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when no scheme was specified in the EventStoreDB connection string.
	/// </summary>
	public class NoSchemeException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="NoSchemeException"/>.
		/// </summary>
		public NoSchemeException()
			: base("Could not parse scheme from connection string") { }
	}
}
