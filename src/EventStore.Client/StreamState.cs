using System;

#nullable enable
namespace EventStore.Client {
	public readonly struct StreamState : IEquatable<StreamState> {
		public static readonly StreamState NoStream = new StreamState(Constants.NoStream);
		public static readonly StreamState Any = new StreamState(Constants.Any);
		public static readonly StreamState StreamExists = new StreamState(Constants.StreamExists);
		private readonly int _value;

		private static class Constants {
			public const int NoStream = 1;
			public const int Any = 2;
			public const int StreamExists = 4;
		}

		internal StreamState(int value) {
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

		public bool Equals(StreamState other) => _value == other._value;
		public override bool Equals(object? obj) => obj is StreamState other && Equals(other);
		public override int GetHashCode() => HashCode.Hash.Combine(_value);
		public static bool operator ==(StreamState left, StreamState right) => left.Equals(right);
		public static bool operator !=(StreamState left, StreamState right) => !left.Equals(right);
		public long ToInt64() => -Convert.ToInt64(_value);
		public static implicit operator int(StreamState streamState) => streamState._value;

		public override string ToString() => _value switch {
			Constants.NoStream => nameof(NoStream),
			Constants.Any => nameof(Any),
			Constants.StreamExists => nameof(StreamExists),
			_ => _value.ToString()
		};
	}
}
