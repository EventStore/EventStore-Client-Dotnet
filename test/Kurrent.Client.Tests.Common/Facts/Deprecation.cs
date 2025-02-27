namespace Kurrent.Client.Tests;

[PublicAPI]
public class Deprecation {
	public class FactAttribute(Version since, string skipMessage) : Xunit.FactAttribute {
		public override string? Skip {
			get => KurrentPermanentTestNode.Version >= since ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute : Xunit.TheoryAttribute {
		readonly Version _legacySince;
		readonly string  _skipMessage;

		public TheoryAttribute(Version since, string skipMessage) {
			_legacySince = since;
			_skipMessage = skipMessage;
		}

		public override string? Skip {
			get => KurrentPermanentTestNode.Version >= _legacySince ? _skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}
