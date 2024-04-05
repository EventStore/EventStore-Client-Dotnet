namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when an invalid setting is found in an EventStoreDB connection string.
/// </summary>
public class InvalidSettingException(string message) : ConnectionStringParseException(message);