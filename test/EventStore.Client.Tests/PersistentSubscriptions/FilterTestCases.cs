using System.Reflection;

namespace EventStore.Client.Tests.PersistentSubscriptions;

public static class Filters {
	const string StreamNamePrefix = nameof(StreamNamePrefix);
	const string StreamNameRegex  = nameof(StreamNameRegex);
	const string EventTypePrefix  = nameof(EventTypePrefix);
	const string EventTypeRegex   = nameof(EventTypeRegex);

	static readonly IDictionary<string, (Func<string, IEventFilter>, Func<string, EventData, EventData>)>
		s_filters =
			new Dictionary<string, (Func<string, IEventFilter>, Func<string, EventData, EventData>)> {
				[StreamNamePrefix] = (StreamFilter.Prefix, (_, e) => e),
				[StreamNameRegex]  = (f => StreamFilter.RegularExpression(f), (_, e) => e),
				[EventTypePrefix] = (EventTypeFilter.Prefix, (term, e) => new(
					e.EventId,
					term,
					e.Data,
					e.Metadata,
					e.ContentType
				)),
				[EventTypeRegex] = (f => EventTypeFilter.RegularExpression(f), (term, e) => new(
					e.EventId,
					term,
					e.Data,
					e.Metadata,
					e.ContentType
				))
			};

	public static readonly IEnumerable<string> All = typeof(Filters)
		.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
		.Where(fi => fi.IsLiteral && !fi.IsInitOnly)
		.Select(fi => (string)fi.GetRawConstantValue()!);

	public static (Func<string, IEventFilter> getFilter, Func<string, EventData, EventData> prepareEvent)
		GetFilter(string name) =>
		s_filters[name];
}
