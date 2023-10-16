using System;

namespace EventStore.Client;

public class Deprecation {
	public class FactAttribute : Xunit.FactAttribute {
		private readonly Version _legacySince;
		private readonly string _skipMessage;

		public FactAttribute(Version since, string skipMessage) {
			_legacySince = since;
			_skipMessage = skipMessage;
		}

		public override string? Skip {
			get => EventStoreTestServer.Version >= _legacySince
				? _skipMessage
				: null;
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute : Xunit.TheoryAttribute {
		private readonly Version _legacySince;
		private readonly string _skipMessage;

		public TheoryAttribute(Version since, string skipMessage) {
			_legacySince = since;
			_skipMessage = skipMessage;
		}

		public override string? Skip {
			get => EventStoreTestServer.Version >= _legacySince
				? _skipMessage
				: null;
			set => throw new NotSupportedException();
		}
	}
}
