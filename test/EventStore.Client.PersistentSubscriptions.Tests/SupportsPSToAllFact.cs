using System;

namespace EventStore.Client {
	internal class SupportsPSToAll {
		private static readonly string SkipMessage =
			"Persistent Subscriptions to $all are not supported on " +
			$"{EventStoreTestServer.Version?.ToString(3) ?? "unknown"}";

		internal class FactAttribute : Regression.FactAttribute {
			public FactAttribute() : base(21, SkipMessage) { }
		}

		internal class TheoryAttribute : Regression.TheoryAttribute {
			public TheoryAttribute() : base(21, SkipMessage) { }
		}
	}
}
