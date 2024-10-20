using AutoFixture;

namespace EventStore.Client.Tests;

public class PrefixFilterExpressionTests() : ValueObjectTests<PrefixFilterExpression>(new ScenarioFixture()) {
	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<PrefixFilterExpression>(composer => composer.FromFactory<string>(value => new(value)));
	}
}
