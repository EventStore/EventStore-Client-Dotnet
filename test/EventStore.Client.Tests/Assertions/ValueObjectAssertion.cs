using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace EventStore.Client;

class ValueObjectAssertion(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(CreateChildrenAssertions(builder)) {
	static IEnumerable<IIdiomaticAssertion> CreateChildrenAssertions(ISpecimenBuilder builder) {
		yield return new EqualityAssertion(builder);
		yield return new ComparableAssertion(builder);
		yield return new StringConversionAssertion(builder);
		yield return new NullArgumentAssertion(builder);
	}
}
