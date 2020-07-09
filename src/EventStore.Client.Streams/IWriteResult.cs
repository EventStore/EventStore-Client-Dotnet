namespace EventStore.Client {
	/// <summary>
	/// An interface representing the result of a write operation.
	/// </summary>
	public interface IWriteResult {
		/// <summary>
		/// The version the stream is currently at.
		/// </summary>
		long NextExpectedVersion { get; }
		/// <summary>
		/// The <see cref="Position"/> of the <see cref="IWriteResult"/> in the transaction file.
		/// </summary>
		Position LogPosition { get; }
	}
}
