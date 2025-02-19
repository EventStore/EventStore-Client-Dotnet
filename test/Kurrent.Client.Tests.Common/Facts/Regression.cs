namespace Kurrent.Client.Tests;

[PublicAPI]
public class Regression {
	public class FactAttribute(int major, string skipMessage) : Xunit.FactAttribute {
		public override string? Skip {
			get => (KurrentPermanentTestNode.Version?.Major ?? int.MaxValue) < major ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute(int major, string skipMessage) : Xunit.TheoryAttribute {
		public override string? Skip {
			get => (KurrentPermanentTestNode.Version?.Major ?? int.MaxValue) < major ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}
