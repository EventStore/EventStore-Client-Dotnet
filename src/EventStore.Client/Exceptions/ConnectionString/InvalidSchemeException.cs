namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when an invalid scheme is defined in the EventStoreDB connection string.
/// </summary>
public class InvalidSchemeException(string scheme, string[] supportedSchemes)
	: ConnectionStringParseException($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}");