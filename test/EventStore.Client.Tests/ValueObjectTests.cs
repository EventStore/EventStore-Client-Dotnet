using AutoFixture;

namespace EventStore.Client.Tests;

public abstract class ValueObjectTests<T>(Fixture fixture) {
	protected readonly Fixture _fixture = fixture;

	[Fact]
	public void ValueObjectIsWellBehaved() => _fixture.Create<ValueObjectAssertion>().Verify(typeof(T));

	[Fact]
	public void ValueObjectIsEquatable() => Assert.IsAssignableFrom<IEquatable<T>>(_fixture.Create<T>());
}
