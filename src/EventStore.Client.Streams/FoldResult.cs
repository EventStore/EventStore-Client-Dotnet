#nullable enable
namespace EventStore.Client {
	/// <summary>
	///  Represents a fold result
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public struct FoldResult<T> {
		/// <summary>
		/// the position of the last event folded
		/// </summary>
		public StreamRevision Revision { get; }
		/// <summary>
		/// the fold value
		/// </summary>
		public T Value { get; }

		/// <summary>
		/// build a fold resuult
		/// </summary>
		/// <param name="revision"></param>
		/// <param name="value"></param>
		public FoldResult(StreamRevision revision, T value) {
			Revision = revision;
			Value = value;
		}
	}
}
