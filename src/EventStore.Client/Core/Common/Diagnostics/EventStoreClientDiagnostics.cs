using System.Diagnostics;

namespace EventStore.Client.Diagnostics;

public static class EventStoreClientDiagnostics {
	public const           string         InstrumentationName = "eventstoredb";
	public static readonly ActivitySource ActivitySource      = new ActivitySource(InstrumentationName);
}