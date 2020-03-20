using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Exception thrown when a required metadata property is missing.
	/// </summary>
	public class RequiredMetadataPropertyMissingException : Exception {
		public RequiredMetadataPropertyMissingException(string missingMetadataProperty,
			Exception? innerException = null) :
			base($"Required metadata property {missingMetadataProperty} is missing", innerException) {
		}
	}
}
