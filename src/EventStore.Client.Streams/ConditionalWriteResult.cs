using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A structure that represents the result of a conditional write.
	/// </summary>
	public readonly struct ConditionalWriteResult : IEquatable<ConditionalWriteResult> {
		/// <summary>
		/// Indicates that the stream the operation is targeting was deleted.
		/// </summary>
		public static readonly ConditionalWriteResult StreamDeleted =
			new ConditionalWriteResult(-1, Position.End, ConditionalWriteStatus.StreamDeleted);

		/// <summary>
		/// The correct expected version to use when writing to the stream next.
		/// </summary>
		public readonly long NextExpectedVersion;

		/// <summary>
		/// The <see cref="Position"/> of the write in the transaction file.
		/// </summary>
		public readonly Position LogPosition;

		/// <summary>
		/// The <see cref="ConditionalWriteStatus"/>.
		/// </summary>
		public readonly ConditionalWriteStatus Status;

		private ConditionalWriteResult(long nextExpectedVersion, Position logPosition,
			ConditionalWriteStatus status = ConditionalWriteStatus.Succeeded) {
			NextExpectedVersion = nextExpectedVersion;
			LogPosition = logPosition;
			Status = status;
		}

		internal static ConditionalWriteResult FromWriteResult(IWriteResult writeResult)
			=> writeResult switch {
				WrongExpectedVersionResult wrongExpectedVersion =>
				new ConditionalWriteResult(wrongExpectedVersion.NextExpectedVersion, Position.End,
					ConditionalWriteStatus.VersionMismatch),
				_ => new ConditionalWriteResult(writeResult.NextExpectedVersion, writeResult.LogPosition)
			};

		internal static ConditionalWriteResult FromWrongExpectedVersion(WrongExpectedVersionException ex)
			=> new ConditionalWriteResult(ex.ExpectedVersion ?? -1, Position.End,
				ConditionalWriteStatus.VersionMismatch);

		/// <inheritdoc />
		public bool Equals(ConditionalWriteResult other) => NextExpectedVersion == other.NextExpectedVersion &&
		                                                    LogPosition.Equals(other.LogPosition) &&
		                                                    Status == other.Status;

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is ConditionalWriteResult other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() =>
			HashCode.Hash.Combine(NextExpectedVersion).Combine(LogPosition).Combine(Status);

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(ConditionalWriteResult left, ConditionalWriteResult right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(ConditionalWriteResult left, ConditionalWriteResult right) =>
			!left.Equals(right);

		/// <inheritdoc />
		public override string ToString() => $"{Status}:{NextExpectedVersion}:{LogPosition}";
	}
}
