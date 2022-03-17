using System;

namespace EventStore.Client {
	/// <summary>
	/// A structure representing the logical position of a subscription to all. />
	/// </summary>
	public readonly struct FromAll : IEquatable<FromAll>, IComparable<FromAll>, IComparable {
		/// <summary>
		/// Represents a <see cref="FromAll"/> when no events have been seen (i.e., the beginning).
		/// </summary>
		public static readonly FromAll Start = new(null);

		/// <summary>
		/// Represents a <see cref="FromAll"/> to receive events written after the subscription is confirmed.
		/// </summary>
		public static readonly FromAll End = new(Position.End);

		/// <summary>
		/// Returns a <see cref="FromAll"/> for the given <see cref="Position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Position"/>.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static FromAll After(Position position) => position == Position.End
			? throw new ArgumentException($"Use '{nameof(FromAll)}.{nameof(End)}.'", nameof(position))
			: new(position);

		private readonly Position? _value;

		private FromAll(Position? value) => _value = value;

		/// <summary>
		/// Converts the <see cref="FromAll"/> to a <see cref="ValueTuple{SubscriptionPosition,SubscriptionPosition}"/>.
		/// It is not meant to be used directly from your code.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public (ulong commitPosition, ulong preparePosition) ToUInt64() => this == Start
			? throw new InvalidOperationException(
				$"{nameof(FromAll)}.{nameof(Start)} may not be converted.")
			: (_value!.Value.CommitPosition, _value!.Value.PreparePosition);

		/// <inheritdoc />
		public bool Equals(FromAll other) => Nullable.Equals(_value, other._value);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is FromAll other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => _value.GetHashCode();

#pragma warning disable CS1591
		public static bool operator ==(FromAll left, FromAll right) =>
			Nullable.Equals<FromAll>(left, right);

		public static bool operator !=(FromAll left, FromAll right) =>
			!Nullable.Equals<FromAll>(left, right);

		public static bool operator >(FromAll left, FromAll right) =>
			left.CompareTo(right) > 0;

		public static bool operator <(FromAll left, FromAll right) =>
			left.CompareTo(right) < 0;

		public static bool operator >=(FromAll left, FromAll right) =>
			left.CompareTo(right) >= 0;

		public static bool operator <=(FromAll left, FromAll right) =>
			left.CompareTo(right) <= 0;
#pragma warning restore CS1591

		/// <inheritdoc />
		public int CompareTo(FromAll other) => (_value, other._value) switch {
			(null, null) => 0,
			(null, _) => -1,
			(_, null) => 1,
			_ => _value.Value.CompareTo(other._value.Value)
		};

		/// <inheritdoc />
		public int CompareTo(object? obj) => obj switch {
			null => 1,
			FromAll other => CompareTo(other),
			_ => throw new ArgumentException($"Object is not a {nameof(FromAll)}"),
		};

		/// <inheritdoc />
		public override string ToString() {
			if (_value is null) {
				return "Start";
			}

			if (_value == Position.End) {
				return "Live";
			}

			return _value.Value.ToString();
		}
	}
}
