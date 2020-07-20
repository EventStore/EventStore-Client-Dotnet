#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// An <see cref="IWriteResult"/> that indicates a failed append to a stream.
	/// </summary>
	public readonly struct WrongExpectedVersionResult : IWriteResult {
		/// <summary>
		/// The name of the stream.
		/// </summary>
		public string StreamName { get; }

		/// <inheritdoc />
		public long NextExpectedVersion { get; }

		/// <summary>
		/// The version the stream is at.
		/// </summary>
		public long ActualVersion { get; }

		/// <inheritdoc />
		public Position LogPosition { get; }

		/// <inheritdoc />
		public StreamRevision NextExpectedStreamRevision { get; }

		/// <summary>
		/// Construct a new <see cref="WrongExpectedVersionResult"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="nextExpectedStreamRevision"></param>
		public WrongExpectedVersionResult(string streamName, StreamRevision nextExpectedStreamRevision) {
			StreamName = streamName;
			ActualVersion = NextExpectedVersion = nextExpectedStreamRevision.ToInt64();
			NextExpectedStreamRevision = nextExpectedStreamRevision;
			LogPosition = default;
		}
	}
}
