using System.Diagnostics;

namespace EventStore.Client.Diagnostics;

public static class KurrentClientDiagnostics {
	public const           string         InstrumentationName = "kurrent";
	public static readonly ActivitySource ActivitySource      = new(InstrumentationName);
}
