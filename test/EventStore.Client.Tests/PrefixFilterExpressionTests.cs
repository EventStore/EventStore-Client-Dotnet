using AutoFixture;

namespace EventStore.Client.Tests;

public class PrefixFilterExpressionTests : ValueObjectTests<PrefixFilterExpression> {
	public PrefixFilterExpressionTests() : base(new ScenarioFixture()) { }

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<PrefixFilterExpression>(composer => composer.FromFactory<string>(value => new(value)));
	}
}