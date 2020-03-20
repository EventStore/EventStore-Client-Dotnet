using System;

#nullable enable
namespace EventStore.Client {
	public class ScavengeNotFoundException : Exception {
		public string? ScavengeId { get; }

		public ScavengeNotFoundException(string? scavengeId, Exception? exception = null) : base(
			$"Scavenge not found. The currently running scavenge is {scavengeId ?? "<unknown>"}.", exception) {
			ScavengeId = scavengeId;
		}
	}
}
