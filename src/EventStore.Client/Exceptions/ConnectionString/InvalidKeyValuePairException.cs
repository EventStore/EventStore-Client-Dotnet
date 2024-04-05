namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when an invalid key value pair is found in an EventStoreDB connection string.
/// </summary>
public class InvalidKeyValuePairException(string keyValuePair) : ConnectionStringParseException($"Invalid key/value pair: '{keyValuePair}'");