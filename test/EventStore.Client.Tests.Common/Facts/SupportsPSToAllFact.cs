namespace EventStore.Client.Tests;

public class SupportsPSToAll {
	const int SupportedFromMajorVersion = 21;

	static readonly string SkipMessage = $"Persistent Subscriptions to $all are not supported on"
	                                   + $" {EventStoreTestServer.Version?.ToString(3) ?? "unknown"}";

	public static bool No  => !Yes;
	public static bool Yes => (EventStoreTestServer.Version?.Major ?? int.MaxValue) >= SupportedFromMajorVersion;

	public class FactAttribute : Regression.FactAttribute {
		public FactAttribute() : base(SupportedFromMajorVersion, SkipMessage) { }
	}

	public class TheoryAttribute : Regression.TheoryAttribute {
		public TheoryAttribute() : base(SupportedFromMajorVersion, SkipMessage) { }
	}
}