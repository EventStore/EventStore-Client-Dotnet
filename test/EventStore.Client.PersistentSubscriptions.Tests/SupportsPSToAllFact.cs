namespace EventStore.Client {
	public class SupportsPSToAll {
		private const int SupportedFromMajorVersion = 21;
		private static readonly string SkipMessage =
			"Persistent Subscriptions to $all are not supported on " +
			$"{EventStoreTestServer.Version?.ToString(3) ?? "unknown"}";

		public class FactAttribute : Regression.FactAttribute {
			public FactAttribute() : base(SupportedFromMajorVersion, SkipMessage) { }
		}

		public class TheoryAttribute : Regression.TheoryAttribute {
			public TheoryAttribute() : base(SupportedFromMajorVersion, SkipMessage) { }
		}

		internal static bool No => !Yes;
		internal static bool Yes => (EventStoreTestServer.Version?.Major ?? int.MaxValue) >= SupportedFromMajorVersion;
	}
}
