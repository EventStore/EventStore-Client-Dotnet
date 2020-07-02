namespace EventStore.Client {
	public class InvalidSchemeException : ConnectionStringParseException {
		public InvalidSchemeException(string scheme, string[] supportedSchemes)
			: base($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}") { }
	}
}
