using System;

#nullable enable
namespace EventStore.Client {
	public readonly struct SuccessResult : IWriteResult, IEquatable<SuccessResult> {
		public long NextExpectedVersion { get; }
		public Position LogPosition { get; }

		public SuccessResult(long nextExpectedVersion, Position logPosition) {
			NextExpectedVersion = nextExpectedVersion;
			LogPosition = logPosition;
		}

		public bool Equals(SuccessResult other) =>
			NextExpectedVersion == other.NextExpectedVersion && LogPosition.Equals(other.LogPosition);

		public override bool Equals(object? obj) => obj is SuccessResult other && Equals(other);
		public static bool operator ==(SuccessResult left, SuccessResult right) => left.Equals(right);
		public static bool operator !=(SuccessResult left, SuccessResult right) => !left.Equals(right);
		public override int GetHashCode() => HashCode.Hash.Combine(NextExpectedVersion).Combine(LogPosition);

		public override string ToString() => $"{NextExpectedVersion}:{LogPosition}";
	}
}
