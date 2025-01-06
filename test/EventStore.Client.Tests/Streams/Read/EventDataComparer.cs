namespace EventStore.Client.Tests;

static class EventDataComparer {
	public static bool Equal(EventData expected, EventRecord actual) {
		if (expected.EventId != actual.EventId)
			return false;

		if (expected.Type != actual.EventType)
			return false;

		return expected.Data.ToArray().SequenceEqual(actual.Data.ToArray()) 
		    && expected.Metadata.ToArray().SequenceEqual(actual.Metadata.ToArray());
	}

	public static bool Equal(EventData[] expected, EventRecord[] actual) {
		if (expected.Length != actual.Length)
			return false;

		return !expected.Where((t, i) => !Equal(t, actual[i])).Any();
	}
}
