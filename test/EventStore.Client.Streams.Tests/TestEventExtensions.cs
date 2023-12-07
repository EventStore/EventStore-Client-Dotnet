namespace EventStore.Client.Streams.Tests;

public static class TestEventExtensions {
	public static IEnumerable<EventRecord> AsResolvedTestEvents(this IEnumerable<ResolvedEvent> events) {
		if (events == null)
			throw new ArgumentNullException(nameof(events));

		return events.Where(x => x.Event.EventType == EventStoreFixture.TestEventType).Select(x => x.Event);
	}
}