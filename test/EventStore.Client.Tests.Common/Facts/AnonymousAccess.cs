namespace EventStore.Client.Tests;

[PublicAPI]
public class AnonymousAccess {
	static readonly Version LegacySince = new(23, 6);
	static readonly string  SkipMessage = "Anonymous access is turned off since v23.6.0!";

	public class FactAttribute() : Deprecation.FactAttribute(LegacySince, SkipMessage);

	public class TheoryAttribute() : Deprecation.TheoryAttribute(LegacySince, SkipMessage);
}
