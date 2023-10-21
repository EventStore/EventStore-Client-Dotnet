using AutoFixture;

namespace EventStore.Client; 

public class PrefixFilterExpressionTests : ValueObjectTests<PrefixFilterExpression> {
    public PrefixFilterExpressionTests() : base(new ScenarioFixture()) { }

    private class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            Customize<PrefixFilterExpression>(composer =>
                                                  composer.FromFactory<string>(value => new PrefixFilterExpression(value)));
        }
    }
}