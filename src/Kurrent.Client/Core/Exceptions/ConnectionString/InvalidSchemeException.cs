namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when an invalid scheme is defined in the KurrentDB connection string.
	/// </summary>
	public class InvalidSchemeException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="InvalidSchemeException"/>.
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="supportedSchemes"></param>
		public InvalidSchemeException(string scheme, string[] supportedSchemes)
			: base($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}") { }
	}
}
