namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when a certificate is invalid or not found in the KurrentDB connection string.
	/// </summary>
	public class InvalidClientCertificateException : ConnectionStringParseException {
		/// <summary>
		/// Constructs a new <see cref="InvalidClientCertificateException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public InvalidClientCertificateException(string message, Exception? innerException = null)
			: base(message, innerException) { }
	}
}
