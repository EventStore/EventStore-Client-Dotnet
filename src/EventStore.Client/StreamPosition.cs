using System;

#nullable enable
namespace EventStore.Client {
	public readonly struct StreamPosition : IEquatable<StreamPosition>, IComparable<StreamPosition> {
		private readonly ulong _value;

		public static readonly StreamPosition Start = new StreamPosition(0);
		public static readonly StreamPosition End = new StreamPosition(ulong.MaxValue);

		public static StreamPosition FromInt64(long value) =>
			value == -1 ? End : new StreamPosition(Convert.ToUInt64(value));

		public StreamPosition(ulong value) {
			if (value > long.MaxValue && value != ulong.MaxValue) {
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			_value = value;
		}

		public int CompareTo(StreamPosition other) => _value.CompareTo(other._value);
		public bool Equals(StreamPosition other) => _value == other._value;
		public override bool Equals(object? obj) => obj is StreamPosition other && Equals(other);
		public override int GetHashCode() => _value.GetHashCode();
		public static bool operator ==(StreamPosition left, StreamPosition right) => left.Equals(right);
		public static bool operator !=(StreamPosition left, StreamPosition right) => !left.Equals(right);

		public static StreamPosition operator +(StreamPosition left, ulong right) {
			checked {
				return new StreamPosition(left._value + right);
			}
		}

		public static StreamPosition operator +(ulong left, StreamPosition right) {
			checked {
				return new StreamPosition(left + right._value);
			}
		}

		public static StreamPosition operator -(StreamPosition left, ulong right) {
			checked {
				return new StreamPosition(left._value - right);
			}
		}

		public static StreamPosition operator -(ulong left, StreamPosition right) {
			checked {
				return new StreamPosition(left - right._value);
			}
		}

		public static bool operator >(StreamPosition left, StreamPosition right) => left._value > right._value;
		public static bool operator <(StreamPosition left, StreamPosition right) => left._value < right._value;
		public static bool operator >=(StreamPosition left, StreamPosition right) => left._value >= right._value;
		public static bool operator <=(StreamPosition left, StreamPosition right) => left._value <= right._value;
		public long ToInt64() => Equals(End) ? -1 : Convert.ToInt64(_value);
		public static implicit operator ulong(StreamPosition streamPosition) => streamPosition._value;
		public static implicit operator StreamPosition(ulong value) => new StreamPosition(value);
		public override string ToString() => this == End ? "End" : _value.ToString();
		public ulong ToUInt64() => _value;
	}
}
