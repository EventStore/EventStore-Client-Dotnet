namespace EventStore.Client {
	public class DuplicateKeyException : ConnectionStringParseException {
		public DuplicateKeyException(string key)
			: base($"Duplicate key: '{key}'") { }
	}
}
