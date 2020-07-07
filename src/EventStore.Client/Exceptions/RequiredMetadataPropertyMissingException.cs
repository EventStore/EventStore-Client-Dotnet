using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Exception thrown when a required metadata property is missing.
	/// </summary>
	public class RequiredMetadataPropertyMissingException : Exception {
		/// <summary>
		/// Constructs a new <see cref="RequiredMetadataPropertyMissingException"/>.
		/// </summary>
		/// <param name="missingMetadataProperty"></param>
		/// <param name="innerException"></param>
		public RequiredMetadataPropertyMissingException(string missingMetadataProperty,
			Exception? innerException = null) :
			base($"Required metadata property {missingMetadataProperty} is missing", innerException) {
		}
	}
}
