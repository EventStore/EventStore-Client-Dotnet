namespace EventStore.Client;

public class AnonymousAccess {
	private static readonly Version LegacySince = new Version(23, 6);
	private static readonly string SkipMessage = 
		"Anonymous access is turned off since v23.6.0!";

	internal class FactAttribute : Deprecation.FactAttribute {
		public FactAttribute() : base(LegacySince, SkipMessage) { }
	}

	internal class TheoryAttribute : Deprecation.TheoryAttribute {
		public TheoryAttribute() : base(LegacySince, SkipMessage) { }
	}
}
