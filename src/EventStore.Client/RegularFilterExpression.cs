using System;
using System.Text.RegularExpressions;

namespace EventStore.Client {
	/// <summary>
	/// A structure representing a regular expression filter.
	/// </summary>
	public readonly struct RegularFilterExpression : IEquatable<RegularFilterExpression> {
		/// <summary>
		/// An empty <see cref="RegularFilterExpression"/>.
		/// </summary>
		public static readonly RegularFilterExpression None = default;

		/// <summary>
		/// A <see cref="RegularFilterExpression"/> that excludes system events (i.e., those whose types start with $).
		/// </summary>
		/// <returns></returns>
		public static readonly RegularFilterExpression ExcludeSystemEvents =
			new RegularFilterExpression(new Regex(@"^[^\$].*"));

		private readonly string? _value;

		/// <summary>
		/// Constructs a new <see cref="RegularFilterExpression"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public RegularFilterExpression(string value) {
			if (value == null) {
				throw new ArgumentNullException(nameof(value));
			}

			_value = value;
		}

		/// <summary>
		/// Constructs a new <see cref="RegularFilterExpression"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public RegularFilterExpression(Regex value) {
			if (value == null) {
				throw new ArgumentNullException(nameof(value));
			}

			_value = value.ToString();
		}

		/// <inheritdoc />
		public bool Equals(RegularFilterExpression other) => string.Equals(_value, other._value);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is RegularFilterExpression other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => _value?.GetHashCode() ?? 0;

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(RegularFilterExpression left, RegularFilterExpression right) =>
			left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(RegularFilterExpression left, RegularFilterExpression right) =>
			!left.Equals(right);

		/// <summary>
		/// Converts a <see cref="RegularFilterExpression"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static implicit operator string?(RegularFilterExpression value) => value._value;

		/// <inheritdoc />
		public override string? ToString() => _value;
	}
}
