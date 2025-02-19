using AutoFixture;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Misc")]
public abstract class ValueObjectTests<T> {
	protected readonly Fixture _fixture;

	protected ValueObjectTests(Fixture fixture) => _fixture = fixture;

	[RetryFact]
	public void ValueObjectIsWellBehaved() => _fixture.Create<ValueObjectAssertion>().Verify(typeof(T));

	[RetryFact]
	public void ValueObjectIsEquatable() => Assert.IsAssignableFrom<IEquatable<T>>(_fixture.Create<T>());
}
