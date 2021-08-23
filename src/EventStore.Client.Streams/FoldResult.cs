#nullable enable
namespace EventStore.Client {
	/// <summary>
	///  Represents the result of a <see cref="EventStoreClient.FoldStreamAsync{T, E}(System.Func{ResolvedEvent, System.Collections.Generic.IEnumerable{E}}, System.Func{T, E, T}, string, StreamPosition, T, System.Action{EventStoreClientOperationOptions}?, bool, UserCredentials?, System.Threading.CancellationToken)" /> call.
	/// </summary>
	/// <typeparam name="T">The type of the aggregated state.</typeparam>
	public struct FoldResult<T> {
		/// <summary>
		/// The position of the last event aggregated.
		/// </summary>
		public StreamRevision Revision { get; }
		/// <summary>
		/// The aggregation result.
		/// </summary>
		public T Value { get; }

		/// <summary>
		/// Build an instance of <see cref="FoldResult{T}"/>.
		/// </summary>
		/// <param name="revision">The last event aggregated.</param>
		/// <param name="value">The aggregation result.</param>
		public FoldResult(StreamRevision revision, T value) {
			Revision = revision;
			Value = value;
		}
	}
}
