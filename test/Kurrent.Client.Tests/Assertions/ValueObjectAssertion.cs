using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace Kurrent.Client;

class ValueObjectAssertion : CompositeIdiomaticAssertion {
	public ValueObjectAssertion(ISpecimenBuilder builder) : base(CreateChildrenAssertions(builder)) { }

	static IEnumerable<IIdiomaticAssertion> CreateChildrenAssertions(ISpecimenBuilder builder) {
		yield return new EqualityAssertion(builder);
		yield return new ComparableAssertion(builder);
		yield return new StringConversionAssertion(builder);
		yield return new NullArgumentAssertion(builder);
	}
}
