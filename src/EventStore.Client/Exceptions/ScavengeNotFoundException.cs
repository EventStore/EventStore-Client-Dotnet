using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when attempting to see the status of a scavenge operation that does not exist.
	/// </summary>
	public class ScavengeNotFoundException : Exception {
		/// <summary>
		/// The id of the scavenge operation.
		/// </summary>
		public string? ScavengeId { get; }

		/// <summary>
		/// Constructs a new <see cref="ScavengeNotFoundException"/>.
		/// </summary>
		/// <param name="scavengeId"></param>
		/// <param name="exception"></param>
		public ScavengeNotFoundException(string? scavengeId, Exception? exception = null) : base(
			$"Scavenge not found. The currently running scavenge is {scavengeId ?? "<unknown>"}.", exception) {
			ScavengeId = scavengeId;
		}
	}
}
