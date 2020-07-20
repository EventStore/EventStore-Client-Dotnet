#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// An enumeration that represents the result of a scavenge operation.
	/// </summary>
	public enum ScavengeResult {
		/// <summary>
		/// The scavenge operation has started.
		/// </summary>
		Started,
		/// <summary>
		/// The scavenge operation is in progress.
		/// </summary>
		InProgress,

		/// <summary>
		/// The scavenge operation has stopped.
		/// </summary>
		Stopped
	}
}
