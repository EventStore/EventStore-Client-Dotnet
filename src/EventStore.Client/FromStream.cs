using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A structure representing the logical position of a subscription. />
	/// </summary>
	public readonly struct FromStream : IEquatable<FromStream>, IComparable<FromStream>, IComparable {
		/// <summary>
		/// Represents a <see cref="FromStream"/> when no events have been seen (i.e., the beginning).
		/// </summary>
		public static readonly FromStream Start = new(null);

		/// <summary>
		/// Represents a <see cref="FromStream"/> to receive events written after the subscription is confirmed.
		/// </summary>
		public static readonly FromStream End = new(StreamPosition.End);

		private readonly StreamPosition? _value;

		/// <summary>
		/// Returns a <see cref="FromStream"/> for the given <see cref="Position"/>.
		/// </summary>
		/// <param name="streamPosition">The <see cref="StreamPosition"/>.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static FromStream After(StreamPosition streamPosition) =>
			streamPosition == StreamPosition.End
				? throw new ArgumentException($"Use '{nameof(FromStream)}.{nameof(End)}.'", nameof(streamPosition))
				: new(streamPosition);

		private FromStream(StreamPosition? value) => _value = value;

		/// <summary>
		/// Converts the <see cref="FromStream"/> to a <see cref="ulong"/>. It is not meant to be used directly from your code.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ulong ToUInt64() => this == Start
			? throw new InvalidOperationException(
				$"{nameof(FromStream)}.{nameof(Start)} may not be converted.")
			: _value!.Value.ToUInt64();

		/// <inheritdoc />
		public bool Equals(FromStream other) => Nullable.Equals(_value, other._value);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is FromStream other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => _value.GetHashCode();

#pragma warning disable CS1591
		public static bool operator ==(FromStream left, FromStream right) =>
			left.Equals(right);

		public static bool operator !=(FromStream left, FromStream right) =>
			!left.Equals(right);

		public static bool operator <(FromStream left, FromStream right) =>
			left.CompareTo(right) < 0;

		public static bool operator >(FromStream left, FromStream right) =>
			left.CompareTo(right) > 0;

		public static bool operator <=(FromStream left, FromStream right) =>
			left.CompareTo(right) <= 0;

		public static bool operator >=(FromStream left, FromStream right) =>
			left.CompareTo(right) >= 0;
#pragma warning restore CS1591

		/// <inheritdoc />
		public int CompareTo(FromStream other) => (_value, other._value) switch {
			(null, null) => 0,
			(null, _) => -1,
			(_, null) => 1,
			_ => _value.Value.CompareTo(other._value.Value)
		};

		/// <inheritdoc />
		public int CompareTo(object? obj) => obj switch {
			null => 1,
			FromStream other => CompareTo(other),
			_ => throw new ArgumentException($"Object is not a {nameof(FromStream)}"),
		};
		
		/// <inheritdoc />
		public override string ToString() {
			if (_value is null) {
				return "Start";
			}

			if (_value == StreamPosition.End) {
				return "Live";
			}

			return _value.Value.ToString();
		}
	}
}
