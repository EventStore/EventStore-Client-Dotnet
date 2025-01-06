using System.Text.RegularExpressions;
using AutoFixture;

namespace EventStore.Client.Tests;

public class RegularFilterExpressionTests : ValueObjectTests<RegularFilterExpression> {
	public RegularFilterExpressionTests() : base(new ScenarioFixture()) { }

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<RegularFilterExpression>(composer => composer.FromFactory<Regex>(value => new(value)));
	}
}
