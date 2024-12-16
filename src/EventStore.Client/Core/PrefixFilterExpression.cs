using System;

namespace EventStore.Client {
	/// <summary>
	/// A structure representing a prefix (i.e., starts with) filter.
	/// </summary>
	public readonly struct PrefixFilterExpression : IEquatable<PrefixFilterExpression> {
		/// <summary>
		/// An empty <see cref="PrefixFilterExpression"/>.
		/// </summary>
		public static readonly PrefixFilterExpression None = default;

		private readonly string? _value;

		/// <summary>
		/// Constructs a new <see cref="PrefixFilterExpression"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public PrefixFilterExpression(string value) {
			if (value == null) {
				throw new ArgumentNullException(nameof(value));
			}

			_value = value;
		}

		/// <inheritdoc />
		public bool Equals(PrefixFilterExpression other) => string.Equals(_value, other._value);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is PrefixFilterExpression other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => _value?.GetHashCode() ?? 0;

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(PrefixFilterExpression left, PrefixFilterExpression right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(PrefixFilterExpression left, PrefixFilterExpression right) =>
			!left.Equals(right);

		/// <summary>
		/// Converts the <see cref="PrefixFilterExpression"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static implicit operator string?(PrefixFilterExpression value) => value._value;

		/// <inheritdoc />
		public override string? ToString() => _value;
	}
}
