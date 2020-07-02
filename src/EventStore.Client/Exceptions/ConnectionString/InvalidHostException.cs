namespace EventStore.Client {
	public class InvalidHostException : ConnectionStringParseException {
		public InvalidHostException(string host)
			: base($"Invalid host: '{host}'") { }
	}
}
