namespace EventStore.Client {
	public class InvalidKeyValuePairException : ConnectionStringParseException {
		public InvalidKeyValuePairException(string keyValuePair)
			: base($"Invalid key/value pair: '{keyValuePair}'") { }
	}
}
