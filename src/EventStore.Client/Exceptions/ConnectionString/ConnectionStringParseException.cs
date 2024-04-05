namespace EventStore.Client;

/// <summary>
/// The base exception that is thrown when an EventStoreDB connection string could not be parsed.
/// </summary>
public class ConnectionStringParseException(string message) : Exception(message);