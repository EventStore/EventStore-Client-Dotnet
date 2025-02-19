namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when a key in the KurrentDB connection string is duplicated.
	/// </summary>
	public class DuplicateKeyException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="DuplicateKeyException"/>.
		/// </summary>
		/// <param name="key"></param>
		public DuplicateKeyException(string key)
			: base($"Duplicate key: '{key}'") { }
	}
}
