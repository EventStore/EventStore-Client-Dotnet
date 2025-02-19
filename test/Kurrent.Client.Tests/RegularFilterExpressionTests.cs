using System.Text.RegularExpressions;
using AutoFixture;
using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Misc")]
public class RegularFilterExpressionTests : ValueObjectTests<RegularFilterExpression> {
	public RegularFilterExpressionTests() : base(new ScenarioFixture()) { }

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<RegularFilterExpression>(composer => composer.FromFactory<Regex>(value => new(value)));
	}
}
