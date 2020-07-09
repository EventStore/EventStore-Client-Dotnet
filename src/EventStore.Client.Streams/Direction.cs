#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// An enumeration that indicates the direction of the read operation.
	/// </summary>
	public enum Direction {
		/// <summary>
		/// Read backwards.
		/// </summary>
		Backwards,

		/// <summary>
		/// Read forwards.
		/// </summary>
		Forwards
	}
}
