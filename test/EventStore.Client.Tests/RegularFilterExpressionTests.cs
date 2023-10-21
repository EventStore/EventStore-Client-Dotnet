using System.Text.RegularExpressions;
using AutoFixture;

namespace EventStore.Client;

public class RegularFilterExpressionTests : ValueObjectTests<RegularFilterExpression> {
    public RegularFilterExpressionTests() : base(new ScenarioFixture()) { }

    private class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            Customize<RegularFilterExpression>(
                composer => composer.FromFactory<Regex>(value => new RegularFilterExpression(value))
            );
        }
    }
}