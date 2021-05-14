using System;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A structure representing a filter on stream names for read operations.
	/// </summary>
	public readonly struct StreamFilter : IEquatable<StreamFilter>, IEventFilter {
		/// <summary>
		/// An empty <see cref="StreamFilter"/>.
		/// </summary>
		public static readonly StreamFilter None = default;

		private readonly PrefixFilterExpression[] _prefixes;

		/// <inheritdoc />
		public PrefixFilterExpression[] Prefixes => _prefixes ?? Array.Empty<PrefixFilterExpression>();

		/// <inheritdoc />
		public RegularFilterExpression Regex { get; }

		/// <inheritdoc />
		public uint? MaxSearchWindow { get; }

		/// <summary>
		/// Creates a <see cref="StreamFilter"/> from a single prefix.
		/// </summary>
		/// <param name="prefix"></param>
		/// <returns></returns>
		public static IEventFilter Prefix(string prefix)
			=> new StreamFilter(new PrefixFilterExpression(prefix));

		/// <summary>
		/// Creates a <see cref="StreamFilter"/> from multiple prefixes.
		/// </summary>
		/// <param name="prefixes"></param>
		/// <returns></returns>
		public static IEventFilter Prefix(params string[] prefixes)
			=> new StreamFilter(Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix)));

		/// <summary>
		/// Creates a <see cref="StreamFilter"/> from a search window and multiple prefixes.
		/// </summary>
		/// <param name="maxSearchWindow"></param>
		/// <param name="prefixes"></param>
		/// <returns></returns>
		public static IEventFilter Prefix(uint maxSearchWindow, params string[] prefixes)
			=> new StreamFilter(maxSearchWindow,
				Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix)));

		/// <summary>
		/// Creates a <see cref="StreamFilter"/> from a regular expression and a search window.
		/// </summary>
		/// <param name="regex"></param>
		/// <param name="maxSearchWindow"></param>
		/// <returns></returns>
		public static IEventFilter RegularExpression(string regex, uint maxSearchWindow = 32)
			=> new StreamFilter(maxSearchWindow, new RegularFilterExpression(regex));

		/// <summary>
		/// Creates a <see cref="StreamFilter"/> from a regular expression and a search window.
		/// </summary>
		/// <param name="regex"></param>
		/// <param name="maxSearchWindow"></param>
		/// <returns></returns>
		public static IEventFilter RegularExpression(Regex regex, uint maxSearchWindow = 32)
			=> new StreamFilter(maxSearchWindow, new RegularFilterExpression(regex));

		private StreamFilter(RegularFilterExpression regex) : this(default, regex) { }

		private StreamFilter(uint maxSearchWindow, RegularFilterExpression regex) {
			if (maxSearchWindow == 0) {
				throw new ArgumentOutOfRangeException(nameof(maxSearchWindow),
					maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0.");
			}

			Regex = regex;
			_prefixes = Array.Empty<PrefixFilterExpression>();
			MaxSearchWindow = maxSearchWindow;
		}

		private StreamFilter(params PrefixFilterExpression[] prefixes) : this(32, prefixes) { }

		private StreamFilter(uint maxSearchWindow, params PrefixFilterExpression[] prefixes) {
			if (prefixes.Length == 0) {
				throw new ArgumentException();
			}

			if (maxSearchWindow == 0) {
				throw new ArgumentOutOfRangeException(nameof(maxSearchWindow),
					maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0.");
			}

			_prefixes = prefixes;
			Regex = RegularFilterExpression.None;
			MaxSearchWindow = maxSearchWindow;
		}

		/// <inheritdoc />
		public bool Equals(StreamFilter other) =>
			Prefixes.SequenceEqual(other.Prefixes) &&
			Regex.Equals(other.Regex) &&
			MaxSearchWindow.Equals(other.MaxSearchWindow);

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is StreamFilter other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Hash.Combine(Prefixes).Combine(Regex).Combine(MaxSearchWindow);

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(StreamFilter left, StreamFilter right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(StreamFilter left, StreamFilter right) => !left.Equals(right);

		/// <inheritdoc />
		public override string ToString() =>
			this == None
				? "(none)"
				: $"{nameof(StreamFilter)} {(Prefixes.Length == 0 ? Regex.ToString() : $"[{string.Join(", ", Prefixes)}]")}";
	}
}
