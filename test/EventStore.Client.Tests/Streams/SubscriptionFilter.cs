// ReSharper disable InconsistentNaming
namespace EventStore.Client.Tests.Streams;

public record SubscriptionFilter(string Name, Func<string, IEventFilter> Create, Func<string, EventData, EventData> PrepareEvent) {
	public override string ToString() => Name;

	static readonly SubscriptionFilter StreamNamePrefix = new(nameof(StreamNamePrefix), StreamFilter.Prefix, (_, evt) => evt);
	static readonly SubscriptionFilter StreamNameRegex  = new(nameof(StreamNameRegex), f => StreamFilter.RegularExpression(f), (_, evt) => evt);
	static readonly SubscriptionFilter EventTypePrefix  = new(nameof(EventTypePrefix), EventTypeFilter.Prefix, (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType));
	static readonly SubscriptionFilter EventTypeRegex   = new(nameof(EventTypeRegex), f => EventTypeFilter.RegularExpression(f), (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType));

	static SubscriptionFilter() {
		All = new[] {
			StreamNamePrefix,
			StreamNameRegex,
			EventTypePrefix,
			EventTypeRegex
		};

		TestCases = All.Select(x => new object[] { x });
	}

	public static SubscriptionFilter[]   All       { get; }
	public static IEnumerable<object?[]> TestCases { get; }
}
