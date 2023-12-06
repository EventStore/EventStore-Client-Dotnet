using System.Reflection;

namespace EventStore.Client.Streams.Tests;

public static class Filters {
	const string StreamNamePrefix = nameof(StreamNamePrefix);
	const string StreamNameRegex  = nameof(StreamNameRegex);
	const string EventTypePrefix  = nameof(EventTypePrefix);
	const string EventTypeRegex   = nameof(EventTypeRegex);

	static readonly SubscriptionFilter StreamNamePrefixSubscriptionFilter = new(StreamNamePrefix, StreamFilter.Prefix, (_, evt) => evt);
	static readonly SubscriptionFilter StreamNameRegexSubscriptionFilter  = new(StreamNameRegex, f => StreamFilter.RegularExpression(f), (_, evt) => evt);
	static readonly SubscriptionFilter EventTypePrefixSubscriptionFilter  = new(EventTypePrefix, EventTypeFilter.Prefix, (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType));
	static readonly SubscriptionFilter EventTypeRegexSubscriptionFilter  = new(EventTypeRegex, f => EventTypeFilter.RegularExpression(f), (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType));
	
	static Filters() {
		EventFilters = new Dictionary<string, SubscriptionFilter> {
			[StreamNamePrefix] = new(StreamNamePrefix, StreamFilter.Prefix, (_, evt) => evt),
			[StreamNameRegex]  = new(StreamNameRegex, f => StreamFilter.RegularExpression(f), (_, evt) => evt),
			[EventTypePrefix]  = new(EventTypePrefix, EventTypeFilter.Prefix, (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType)),
			[EventTypeRegex]   = new(EventTypeRegex, f => EventTypeFilter.RegularExpression(f), (term, evt) => new(evt.EventId, term, evt.Data, evt.Metadata, evt.ContentType))
		};
		
		TestCases =  EventFilters.Select(x => new object[] { x.Value });
	}

	public static IDictionary<string, SubscriptionFilter> EventFilters { get; }
	public static IEnumerable<object?[]>                TestCases    { get; }

	public static readonly IEnumerable<string> All = typeof(Filters)
		.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
		.Where(fi => fi.IsLiteral && !fi.IsInitOnly)
		.Select(fi => (string)fi.GetRawConstantValue()!);
	
	public static (Func<string, IEventFilter> GetFilter, Func<string, EventData, EventData> PrepareEvent) GetFilterDefinition(string name) {
		var result = EventFilters[name];
		return (result.Create, result.PrepareEvent);
	}
}