using System;

namespace EventStore.Client {
	/// <summary>
	/// An <see cref="IWriteResult"/> that indicates a successful append to a stream.
	/// </summary>
	public readonly struct SuccessResult : IWriteResult, IEquatable<SuccessResult> {
		/// <inheritdoc />
		public Position LogPosition { get; }

		/// <inheritdoc />
		public StreamRevision NextExpectedStreamRevision { get; }

		/// <summary>
		/// Constructs a new <see cref="SuccessResult"/>.
		/// </summary>
		/// <param name="nextExpectedStreamRevision"></param>
		/// <param name="logPosition"></param>
		public SuccessResult(StreamRevision nextExpectedStreamRevision, Position logPosition) {
			NextExpectedStreamRevision = nextExpectedStreamRevision;
			LogPosition = logPosition;
			nextExpectedStreamRevision.ToInt64();
		}

		/// <inheritdoc />
		public bool Equals(SuccessResult other) =>
			NextExpectedStreamRevision == other.NextExpectedStreamRevision && LogPosition.Equals(other.LogPosition);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is SuccessResult other && Equals(other);

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(SuccessResult left, SuccessResult right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal not to right.</returns>
		public static bool operator !=(SuccessResult left, SuccessResult right) => !left.Equals(right);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Hash.Combine(LogPosition);

		/// <inheritdoc />
		public override string ToString() => $"{NextExpectedStreamRevision}:{LogPosition}";
	}
}
