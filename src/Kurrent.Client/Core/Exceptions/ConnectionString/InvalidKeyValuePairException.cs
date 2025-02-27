namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when an invalid key value pair is found in an KurrentDB connection string.
	/// </summary>
	public class InvalidKeyValuePairException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="InvalidKeyValuePairException"/>.
		/// </summary>
		/// <param name="keyValuePair"></param>
		public InvalidKeyValuePairException(string keyValuePair)
			: base($"Invalid key/value pair: '{keyValuePair}'") { }
	}
}
