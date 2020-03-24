#nullable enable
namespace EventStore.Client {
	public interface IEventFilter {
		PrefixFilterExpression[] Prefixes { get; }
		RegularFilterExpression Regex { get; }
		uint? MaxSearchWindow { get; }
	}
}
