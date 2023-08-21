using System;

namespace EventStore.Client;

internal class Deprecation {
	internal class FactAttribute : Xunit.FactAttribute {
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

	internal class TheoryAttribute : Xunit.TheoryAttribute {
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
