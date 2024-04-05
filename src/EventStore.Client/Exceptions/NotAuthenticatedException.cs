namespace EventStore.Client;

/// <summary>
/// The exception that is thrown when a user is not authenticated.
/// </summary>
public class NotAuthenticatedException(string message, Exception? innerException = null) : Exception(message, innerException);