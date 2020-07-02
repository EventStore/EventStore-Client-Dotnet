namespace EventStore.Client {
	public class NoSchemeException : ConnectionStringParseException {
		public NoSchemeException()
			: base("Could not parse scheme from connection string") { }
	}
}
