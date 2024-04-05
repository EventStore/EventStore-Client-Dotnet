namespace EventStore.Client;

/// <summary>
/// A structure referring to the expected stream revision when writing to a stream.
/// </summary>
public readonly struct StreamRevision : IEquatable<StreamRevision>, IComparable<StreamRevision>, IComparable {
	readonly ulong _value;

	/// <summary>
	/// Represents no <see cref="StreamRevision"/>, i.e., when a stream does not exist.
	/// </summary>
	public static readonly StreamRevision None = new(ulong.MaxValue);

	/// <summary>
	/// Converts a <see cref="long"/> to a <see cref="StreamRevision"/>.  It is not meant to be used directly from your code.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static StreamRevision FromInt64(long value) =>
		value == -1 ? None : new StreamRevision(Convert.ToUInt64(value));

	/// <summary>
	/// Creates a new <see cref="StreamRevision"/> from the given <see cref="StreamPosition"/>.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public static StreamRevision FromStreamPosition(StreamPosition position) => position.ToUInt64() switch {
		ulong.MaxValue => throw new ArgumentOutOfRangeException(nameof(position)),
		_              => new StreamRevision(position.ToUInt64())
	};

	/// <summary>
	/// Constructs a new <see cref="StreamRevision"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public StreamRevision(ulong value) {
		if (value > long.MaxValue && value != ulong.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));

		_value = value;
	}

	/// <summary>
	/// Advances the <see cref="StreamRevision"/> to the next revision.
	/// </summary>
	/// <returns></returns>
	public StreamRevision Next() => this + 1;

	/// <inheritdoc />
	public int CompareTo(StreamRevision other) => _value.CompareTo(other._value);

	/// <inheritdoc />
	public int CompareTo(object? obj) => obj switch {
		null                 => 1,
		StreamRevision other => CompareTo(other),
		_                    => throw new ArgumentException("Object is not a StreamRevision")
	};

	/// <inheritdoc />
	public bool Equals(StreamRevision other) => _value == other._value;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is StreamRevision other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => _value.GetHashCode();

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(StreamRevision left, StreamRevision right) => left.Equals(right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(StreamRevision left, StreamRevision right) => !left.Equals(right);

	/// <summary>
	/// Adds right to left.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static StreamRevision operator +(StreamRevision left, ulong right) {
		checked {
			return new StreamRevision(left._value + right);
		}
	}

	/// <summary>
	/// Adds right to left.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static StreamRevision operator +(ulong left, StreamRevision right) {
		checked {
			return new StreamRevision(left + right._value);
		}
	}

	/// <summary>
	/// Subtracts right from left.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static StreamRevision operator -(StreamRevision left, ulong right) {
		checked {
			return new StreamRevision(left._value - right);
		}
	}

	/// <summary>
	/// Subtracts right from left.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static StreamRevision operator -(ulong left, StreamRevision right) {
		checked {
			return new StreamRevision(left - right._value);
		}
	}

	/// <summary>
	/// Compares whether left &gt; right.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator >(StreamRevision left, StreamRevision right) => left._value > right._value;

	/// <summary>
	/// Compares whether left &lt; right.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator <(StreamRevision left, StreamRevision right) => left._value < right._value;

	/// <summary>
	/// Compares whether left &gt;= right.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator >=(StreamRevision left, StreamRevision right) => left._value >= right._value;

	/// <summary>
	/// Compares whether left &lt;= right.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator <=(StreamRevision left, StreamRevision right) => left._value <= right._value;

	/// <summary>
	/// Converts the <see cref="StreamRevision"/> to a <see cref="long"/>. It is not meant to be used directly from your code.
	/// </summary>
	/// <returns></returns>
	public long ToInt64() => Equals(None) ? -1 : Convert.ToInt64(_value);

	/// <summary>
	/// Converts a <see cref="StreamRevision"/> to a <see cref="ulong" />.
	/// </summary>
	/// <param name="streamRevision"></param>
	/// <returns></returns>
	public static implicit operator ulong(StreamRevision streamRevision) => streamRevision._value;

	/// <summary>
	/// Converts a <see cref="ulong"/> to a <see cref="StreamRevision" />.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator StreamRevision(ulong value) => new(value);

	/// <inheritdoc />
	public override string ToString() => this == None ? nameof(None) : _value.ToString();

	/// <summary>
	/// Converts the <see cref="StreamRevision"/> to a <see cref="ulong" />.
	/// </summary>
	/// <returns></returns>
	public ulong ToUInt64() => _value;
}