namespace EventStore.Client {
	/// <summary>
	/// An enumeration representing the state of a read operation.
	/// </summary>
	public enum ReadState {
		/// <summary>
		/// The stream does not exist.
		/// </summary>
		StreamNotFound,
		/// <summary>
		/// The stream exists.
		/// </summary>
		Ok
	}
}
