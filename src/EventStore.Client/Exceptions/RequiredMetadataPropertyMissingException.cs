namespace EventStore.Client;

/// <summary>
/// Exception thrown when a required metadata property is missing.
/// </summary>
public class RequiredMetadataPropertyMissingException(string missingMetadataProperty, Exception? innerException = null) 
	: Exception($"Required metadata property {missingMetadataProperty} is missing", innerException);