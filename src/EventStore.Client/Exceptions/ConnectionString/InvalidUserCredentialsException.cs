namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when an invalid <see cref="UserCredentials"/> is specified in the EventStoreDB connection string.
/// </summary>
public class InvalidUserCredentialsException(string userInfo)
	: ConnectionStringParseException($"Invalid user credentials: '{userInfo}'. Username & password must be delimited by a colon");