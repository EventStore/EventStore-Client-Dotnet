using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A structure referring to an <see cref="EventRecord"/>'s position within a stream.
	/// </summary>
	public readonly struct StreamPosition : IEquatable<StreamPosition>, IComparable<StreamPosition> {
		private readonly ulong _value;

		/// <summary>
		/// The beginning (i.e., the first event) of a stream.
		/// </summary>
		public static readonly StreamPosition Start = new StreamPosition(0);

		/// <summary>
		/// The end of a stream. Use this when reading a stream backwards, or subscribing live to a stream.
		/// </summary>
		public static readonly StreamPosition End = new StreamPosition(ulong.MaxValue);

		/// <summary>
		/// Converts a <see cref="long"/> to a <see cref="StreamPosition"/>.  It is not meant to be used directly from your code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static StreamPosition FromInt64(long value) =>
			value == -1 ? End : new StreamPosition(Convert.ToUInt64(value));

		/// <summary>
		/// Constructs a new <see cref="StreamPosition"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public StreamPosition(ulong value) {
			if (value > long.MaxValue && value != ulong.MaxValue) {
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			_value = value;
		}

		/// <inheritdoc />
		public int CompareTo(StreamPosition other) => _value.CompareTo(other._value);

		/// <inheritdoc />
		public bool Equals(StreamPosition other) => _value == other._value;

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is StreamPosition other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => _value.GetHashCode();

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(StreamPosition left, StreamPosition right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(StreamPosition left, StreamPosition right) => !left.Equals(right);

		/// <summary>
		/// Adds right to left.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static StreamPosition operator +(StreamPosition left, ulong right) {
			checked {
				return new StreamPosition(left._value + right);
			}
		}

		/// <summary>
		/// Adds right to left.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static StreamPosition operator +(ulong left, StreamPosition right) {
			checked {
				return new StreamPosition(left + right._value);
			}
		}

		/// <summary>
		/// Subtracts right from left.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static StreamPosition operator -(StreamPosition left, ulong right) {
			checked {
				return new StreamPosition(left._value - right);
			}
		}

		/// <summary>
		/// Subtracts right from left.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static StreamPosition operator -(ulong left, StreamPosition right) {
			checked {
				return new StreamPosition(left - right._value);
			}
		}

		/// <summary>
		/// Compares whether left &gt; right.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >(StreamPosition left, StreamPosition right) => left._value > right._value;

		/// <summary>
		/// Compares whether left &lt; right.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <(StreamPosition left, StreamPosition right) => left._value < right._value;

		/// <summary>
		/// Compares whether left &gt;= right.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >=(StreamPosition left, StreamPosition right) => left._value >= right._value;

		/// <summary>
		/// Compares whether left &lt;= right.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <=(StreamPosition left, StreamPosition right) => left._value <= right._value;

		/// <summary>
		/// Converts the <see cref="StreamPosition"/> to a <see cref="long"/>. It is not meant to be used directly from your code.
		/// </summary>
		/// <returns></returns>
		public long ToInt64() => Equals(End) ? -1 : Convert.ToInt64(_value);

		/// <summary>
		/// Converts a <see cref="StreamPosition"/> to a <see cref="ulong" />.
		/// </summary>
		/// <param name="streamPosition"></param>
		/// <returns></returns>
		public static implicit operator ulong(StreamPosition streamPosition) => streamPosition._value;

		/// <summary>
		/// Converts a <see cref="ulong"/> to a <see cref="StreamPosition" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static implicit operator StreamPosition(ulong value) => new StreamPosition(value);

		/// <inheritdoc />
		public override string ToString() => this == End ? "End" : _value.ToString();

		/// <summary>
		/// Converts the <see cref="StreamPosition"/> to a <see cref="ulong" />.
		/// </summary>
		/// <returns></returns>
		public ulong ToUInt64() => _value;
	}
}
