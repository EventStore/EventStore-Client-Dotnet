using System.Text.RegularExpressions;

namespace EventStore.Client;

/// <summary>
/// A structure representing a filter on event types for read operations.
/// </summary>
public readonly struct EventTypeFilter : IEquatable<EventTypeFilter>, IEventFilter {
	/// <summary>
	/// An empty <see cref="EventTypeFilter"/>.
	/// </summary>
	public static readonly EventTypeFilter None = default;

	readonly PrefixFilterExpression[] _prefixes;

	/// <inheritdoc />
	public PrefixFilterExpression[] Prefixes => _prefixes ?? [];

	/// <inheritdoc />
	public RegularFilterExpression Regex { get; }

	/// <inheritdoc />
	public uint? MaxSearchWindow { get; }

	/// <summary>
	/// An <see cref="EventTypeFilter"/> that excludes system events (i.e., those whose types start with $).
	/// </summary>
	/// <param name="maxSearchWindow"></param>
	/// <returns></returns>
	public static EventTypeFilter ExcludeSystemEvents(uint maxSearchWindow = 32) =>
		new(maxSearchWindow, RegularFilterExpression.ExcludeSystemEvents);

	/// <summary>
	/// Creates an <see cref="EventTypeFilter"/> from a single prefix.
	/// </summary>
	/// <param name="prefix"></param>
	/// <returns></returns>
	public static IEventFilter Prefix(string prefix)
		=> new EventTypeFilter(new PrefixFilterExpression(prefix));

	/// <summary>
	/// Creates an <see cref="EventTypeFilter"/> from multiple prefixes.
	/// </summary>
	/// <param name="prefixes"></param>
	/// <returns></returns>
	public static IEventFilter Prefix(params string[] prefixes)
		=> new EventTypeFilter(Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix)));

	/// <summary>
	/// Creates an <see cref="EventTypeFilter"/> from a search window and multiple prefixes.
	/// </summary>
	/// <param name="maxSearchWindow"></param>
	/// <param name="prefixes"></param>
	/// <returns></returns>
	public static IEventFilter Prefix(uint maxSearchWindow, params string[] prefixes)
		=> new EventTypeFilter(
			maxSearchWindow,
			Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix))
		);

	/// <summary>
	/// Creates an <see cref="EventTypeFilter"/> from a regular expression and a search window.
	/// </summary>
	/// <param name="regex"></param>
	/// <param name="maxSearchWindow"></param>
	/// <returns></returns>
	public static IEventFilter RegularExpression(string regex, uint maxSearchWindow = 32)
		=> new EventTypeFilter(maxSearchWindow, new RegularFilterExpression(regex));

	/// <summary>
	/// Creates an <see cref="EventTypeFilter"/> from a regular expression and a search window.
	/// </summary>
	/// <param name="regex"></param>
	/// <param name="maxSearchWindow"></param>
	/// <returns></returns>
	public static IEventFilter RegularExpression(Regex regex, uint maxSearchWindow = 32)
		=> new EventTypeFilter(maxSearchWindow, new RegularFilterExpression(regex));

	EventTypeFilter(uint maxSearchWindow, RegularFilterExpression regex) {
		if (maxSearchWindow == 0)
			throw new ArgumentOutOfRangeException(
				nameof(maxSearchWindow),
				maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0."
			);

		Regex           = regex;
		_prefixes       = [];
		MaxSearchWindow = maxSearchWindow;
	}

	EventTypeFilter(params PrefixFilterExpression[] prefixes) : this(32, prefixes) { }

	EventTypeFilter(uint maxSearchWindow, params PrefixFilterExpression[] prefixes) {
		if (prefixes.Length == 0) throw new ArgumentException();

		if (maxSearchWindow == 0)
			throw new ArgumentOutOfRangeException(
				nameof(maxSearchWindow),
				maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0."
			);

		_prefixes       = prefixes;
		Regex           = RegularFilterExpression.None;
		MaxSearchWindow = maxSearchWindow;
	}

	/// <inheritdoc />
	public bool Equals(EventTypeFilter other) =>
		Prefixes == null || other.Prefixes == null
			? Prefixes == other.Prefixes &&
			  Regex.Equals(other.Regex) &&
			  MaxSearchWindow.Equals(other.MaxSearchWindow)
			: Prefixes.SequenceEqual(other.Prefixes) &&
			  Regex.Equals(other.Regex) &&
			  MaxSearchWindow.Equals(other.MaxSearchWindow);

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is EventTypeFilter other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(Prefixes).Combine(Regex).Combine(MaxSearchWindow);

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(EventTypeFilter left, EventTypeFilter right) => left.Equals(right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(EventTypeFilter left, EventTypeFilter right) => !left.Equals(right);

	/// <inheritdoc />
	public override string ToString() =>
		this == None
			? "(none)"
			: $"{nameof(EventTypeFilter)} {(Prefixes.Length == 0 ? Regex.ToString() : $"[{string.Join(", ", Prefixes)}]")}";
}