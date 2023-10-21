using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace EventStore.Client; 

internal class EqualityAssertion : CompositeIdiomaticAssertion {
    public EqualityAssertion(ISpecimenBuilder builder) : base(CreateChildrenAssertions(builder)) { }

    private static IEnumerable<IIdiomaticAssertion> CreateChildrenAssertions(ISpecimenBuilder builder) {
        yield return new EqualsNewObjectAssertion(builder);
        yield return new EqualsSelfAssertion(builder);
        yield return new EqualsSuccessiveAssertion(builder);
        yield return new GetHashCodeSuccessiveAssertion(builder);
        yield return new SameValueEqualityOperatorsAssertion(builder);
        yield return new DifferentValuesEqualityOperatorsAssertion(builder);
    }

    private class SameValueEqualityOperatorsAssertion : IdiomaticAssertion {
        private readonly ISpecimenBuilder _builder;

        public SameValueEqualityOperatorsAssertion(ISpecimenBuilder builder) =>
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        public override void Verify(Type type) {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var instance = new SpecimenContext(_builder).Resolve(type);

            var equals    = type.InvokeEqualityOperator(instance, instance);
            var notEquals = type.InvokeInequalityOperator(instance, instance);

            if (equals == notEquals) {
                throw new Exception(
                    $"The type '{type}' returned {equals} for both equality (==) and inequality (!=).");
            }

            if (!equals) {
                throw new Exception($"The type '{type}' did not implement the equality (==) operator correctly.");
            }

            if (notEquals) {
                throw new Exception($"The type '{type}' did not implement the inequality (!=) operator correctly.");
            }
        }
    }

    private class DifferentValuesEqualityOperatorsAssertion : IdiomaticAssertion {
        private readonly ISpecimenBuilder _builder;

        public DifferentValuesEqualityOperatorsAssertion(ISpecimenBuilder builder) =>
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        public override void Verify(Type type) {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var context  = new SpecimenContext(_builder);
            var instance = context.Resolve(type);
            var other    = context.Resolve(type);

            var equals    = type.InvokeEqualityOperator(instance, other);
            var notEquals = type.InvokeInequalityOperator(instance, other);

            if (equals == notEquals) {
                throw new Exception(
                    $"The type '{type}' returned {equals} for both equality (==) and inequality (!=).");
            }

            if (equals) {
                throw new Exception($"The type '{type}' did not implement the equality (==) operator correctly.");
            }

            if (!notEquals) {
                throw new Exception($"The type '{type}' did not implement the inequality (!=) operator correctly.");
            }

        }
    }
}