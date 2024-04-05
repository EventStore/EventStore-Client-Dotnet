namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when a certificate is invalid or not found in the EventStoreDB connection string.
/// </summary>
public class InvalidClientCertificateException(string message) : ConnectionStringParseException(message);