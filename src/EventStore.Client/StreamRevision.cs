using System;

#nullable enable
namespace EventStore.Client {
	public readonly struct StreamRevision : IEquatable<StreamRevision>, IComparable<StreamRevision> {
		private readonly ulong _value;

		public static readonly StreamRevision None = new StreamRevision(ulong.MaxValue);

		public static StreamRevision FromInt64(long value) =>
			value == -1 ? None : new StreamRevision(Convert.ToUInt64(value));

		public StreamRevision(ulong value) {
			if (value > long.MaxValue && value != ulong.MaxValue) {
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			_value = value;
		}

		public int CompareTo(StreamRevision other) => _value.CompareTo(other._value);
		public bool Equals(StreamRevision other) => _value == other._value;
		public override bool Equals(object? obj) => obj is StreamRevision other && Equals(other);
		public override int GetHashCode() => _value.GetHashCode();
		public static bool operator ==(StreamRevision left, StreamRevision right) => left.Equals(right);
		public static bool operator !=(StreamRevision left, StreamRevision right) => !left.Equals(right);

		public static StreamRevision operator +(StreamRevision left, ulong right) {
			checked {
				return new StreamRevision(left._value + right);
			}
		}

		public static StreamRevision operator +(ulong left, StreamRevision right) {
			checked {
				return new StreamRevision(left + right._value);
			}
		}

		public static StreamRevision operator -(StreamRevision left, ulong right) {
			checked {
				return new StreamRevision(left._value - right);
			}
		}

		public static StreamRevision operator -(ulong left, StreamRevision right) {
			checked {
				return new StreamRevision(left - right._value);
			}
		}

		public static bool operator >(StreamRevision left, StreamRevision right) => left._value > right._value;
		public static bool operator <(StreamRevision left, StreamRevision right) => left._value < right._value;
		public static bool operator >=(StreamRevision left, StreamRevision right) => left._value >= right._value;
		public static bool operator <=(StreamRevision left, StreamRevision right) => left._value <= right._value;
		public long ToInt64() => Equals(None) ? -1 : Convert.ToInt64(_value);
		public static implicit operator ulong(StreamRevision streamRevision) => streamRevision._value;
		public override string ToString() => this == None ? "End" : _value.ToString();
		public ulong ToUInt64() => _value;
	}
}
