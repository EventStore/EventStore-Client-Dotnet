namespace EventStore.Client.Tests;

public class Regression {
	public class FactAttribute : Xunit.FactAttribute {
		readonly int    _major;
		readonly string _skipMessage;

		public FactAttribute(int major, string skipMessage) {
			_major       = major;
			_skipMessage = skipMessage;
		}

		public override string? Skip {
			get => (EventStoreTestServer.Version?.Major ?? int.MaxValue) < _major ? _skipMessage : null;
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute : Xunit.TheoryAttribute {
		readonly int    _major;
		readonly string _skipMessage;

		public TheoryAttribute(int major, string skipMessage) {
			_major       = major;
			_skipMessage = skipMessage;
		}

		public override string? Skip {
			get => (EventStoreTestServer.Version?.Major ?? int.MaxValue) < _major ? _skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}