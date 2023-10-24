using AutoFixture;

namespace EventStore.Client;

public abstract class ValueObjectTests<T> {
    protected readonly Fixture _fixture;

    protected ValueObjectTests(Fixture fixture) => _fixture = fixture;

    [Fact]
    public void ValueObjectIsWellBehaved() => _fixture.Create<ValueObjectAssertion>().Verify(typeof(T));

    [Fact]
    public void ValueObjectIsEquatable() => Assert.IsAssignableFrom<IEquatable<T>>(_fixture.Create<T>());
}