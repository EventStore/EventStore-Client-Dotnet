using System;

#nullable enable
namespace EventStore.Client {
	public readonly struct AnyStreamRevision : IEquatable<AnyStreamRevision> {
		public static readonly AnyStreamRevision NoStream = new AnyStreamRevision(Constants.NoStream);
		public static readonly AnyStreamRevision Any = new AnyStreamRevision(Constants.Any);
		public static readonly AnyStreamRevision StreamExists = new AnyStreamRevision(Constants.StreamExists);
		private readonly int _value;

		private static class Constants {
			public const int NoStream = 1;
			public const int Any = 2;
			public const int StreamExists = 4;
		}

		internal AnyStreamRevision(int value) {
			switch (value) {
				case Constants.NoStream:
				case Constants.Any:
				case Constants.StreamExists:
					_value = value;
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(value));
			}
		}

		public bool Equals(AnyStreamRevision other) => _value == other._value;
		public override bool Equals(object? obj) => obj is AnyStreamRevision other && Equals(other);
		public override int GetHashCode() => HashCode.Hash.Combine(_value);
		public static bool operator ==(AnyStreamRevision left, AnyStreamRevision right) => left.Equals(right);
		public static bool operator !=(AnyStreamRevision left, AnyStreamRevision right) => !left.Equals(right);
		public long ToInt64() => -Convert.ToInt64(_value);
		public static implicit operator int(AnyStreamRevision streamRevision) => streamRevision._value;

		public override string ToString() => _value switch {
			Constants.NoStream => nameof(NoStream),
			Constants.Any => nameof(Any),
			Constants.StreamExists => nameof(StreamExists),
			_ => _value.ToString()
		};
	}
}
