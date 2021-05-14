#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// An interface that represents a search filter, used for read operations.
	/// </summary>
	public interface IEventFilter {
		/// <summary>
		/// The <see cref="PrefixFilterExpression"/>s associated with this <see cref="IEventFilter"/>.
		/// </summary>
		PrefixFilterExpression[] Prefixes { get; }

		/// <summary>
		/// The <see cref="RegularFilterExpression"/> associated with this <see cref="EventTypeFilter"/>.
		/// </summary>
		RegularFilterExpression Regex { get; }

		/// <summary>
		/// The maximum number of events to read that do not match the filter before the operation returns.
		/// </summary>
		uint? MaxSearchWindow { get; }
	}
}
