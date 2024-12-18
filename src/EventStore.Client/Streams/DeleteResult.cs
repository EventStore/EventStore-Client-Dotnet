using System;

namespace EventStore.Client {
	/// <summary>
	/// A structure that represents the result of a delete operation.
	/// </summary>
	public readonly struct DeleteResult : IEquatable<DeleteResult> {
		/// <inheritdoc />
		public bool Equals(DeleteResult other) => LogPosition.Equals(other.LogPosition);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is DeleteResult other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => LogPosition.GetHashCode();

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(DeleteResult left, DeleteResult right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(DeleteResult left, DeleteResult right) => !left.Equals(right);

		/// <summary>
		/// The <see cref="Position"/> of the delete in the transaction file.
		/// </summary>
		public readonly Position LogPosition;

		/// <summary>
		/// Constructs a new <see cref="DeleteResult"/>.
		/// </summary>
		/// <param name="logPosition"></param>
		public DeleteResult(Position logPosition) {
			LogPosition = logPosition;
		}
	}
}
