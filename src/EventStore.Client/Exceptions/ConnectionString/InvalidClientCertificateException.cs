namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when a certificate is invalid or not found in the EventStoreDB connection string.
	/// </summary>
	public class InvalidClientCertificateException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="InvalidClientCertificateException"/>.
		/// </summary>
		/// <param name="message"></param>
		public InvalidClientCertificateException(string message)
			: base(message) { }
	}
}
