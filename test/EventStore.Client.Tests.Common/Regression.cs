using System;

namespace EventStore.Client {
	public class Regression {
		public class FactAttribute : Xunit.FactAttribute {
			private readonly int _major;
			private readonly string _skipMessage;

			public FactAttribute(int major, string skipMessage) {
				_major = major;
				_skipMessage = skipMessage;
			}

			public override string? Skip {
				get => (EventStoreTestServer.Version?.Major ?? int.MaxValue) < _major
					? _skipMessage
					: null;
				set => throw new NotSupportedException();
			}
		}

		public class TheoryAttribute : Xunit.TheoryAttribute {
			private readonly int _major;
			private readonly string _skipMessage;

			public TheoryAttribute(int major, string skipMessage) {
				_major = major;
				_skipMessage = skipMessage;
			}

			public override string? Skip {
				get => (EventStoreTestServer.Version?.Major ?? int.MaxValue) < _major
					? _skipMessage
					: null;
				set => throw new NotSupportedException();
			}
		}
	}
}
