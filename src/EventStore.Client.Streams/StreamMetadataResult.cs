using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Represents stream metadata as a series of properties for system
	/// data (e.g., MaxAge) and a <see cref="StreamMetadata"/> object for user metadata.
	/// </summary>
	public struct StreamMetadataResult : IEquatable<StreamMetadataResult> {
		/// <summary>
		/// The name of the stream.
		/// </summary>
		public readonly string StreamName;

		/// <summary>
		/// True if the stream is deleted.
		/// </summary>
		public readonly bool StreamDeleted;

		/// <summary>
		/// A <see cref="StreamMetadata"/> containing user-specified metadata.
		/// </summary>
		public readonly StreamMetadata Metadata;

		/// <summary>
		/// A <see cref="StreamPosition"/> of the version of the metadata.
		/// </summary>
		public readonly StreamPosition? MetastreamRevision;

		/// <inheritdoc />
		public override int GetHashCode() =>
			HashCode.Hash.Combine(StreamName).Combine(Metadata).Combine(MetastreamRevision);

		/// <inheritdoc />
		public bool Equals(StreamMetadataResult other) =>
			StreamName == other.StreamName && StreamDeleted == other.StreamDeleted &&
			Equals(Metadata, other.Metadata) && Nullable.Equals(MetastreamRevision, other.MetastreamRevision);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is StreamMetadataResult other && Equals(other);

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(StreamMetadataResult left, StreamMetadataResult right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(StreamMetadataResult left, StreamMetadataResult right) => !left.Equals(right);

		/// <summary>
		/// A <see cref="StreamMetadataResult"/> representing no metadata.
		/// </summary>
		/// <param name="streamName"></param>
		/// <returns></returns>
		public static StreamMetadataResult None(string streamName) => new StreamMetadataResult(streamName);

		/// <summary>
		/// A factory method to create a new <see cref="StreamMetadataResult"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="revision"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static StreamMetadataResult Create(string streamName, StreamPosition revision,
			StreamMetadata metadata) {
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			return new StreamMetadataResult(streamName, revision, metadata);
		}

		private StreamMetadataResult(string streamName, StreamPosition? metastreamRevision = null,
			StreamMetadata metadata = default, bool streamDeleted = false) {
			if (streamName == null) {
				throw new ArgumentNullException(nameof(streamName));
			}

			StreamName = streamName;
			StreamDeleted = streamDeleted;
			Metadata = metadata;
			MetastreamRevision = metastreamRevision;
		}
	}
}
