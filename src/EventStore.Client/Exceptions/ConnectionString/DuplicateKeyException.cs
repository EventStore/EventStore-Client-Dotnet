namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when a key in the EventStoreDB connection string is duplicated.
/// </summary>
public class DuplicateKeyException(string key) : ConnectionStringParseException($"Duplicate key: '{key}'");