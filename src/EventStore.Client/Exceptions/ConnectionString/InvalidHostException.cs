namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when there is an invalid host in the EventStoreDB connection string.
/// </summary>
public class InvalidHostException(string host) : ConnectionStringParseException($"Invalid host: '{host}'");