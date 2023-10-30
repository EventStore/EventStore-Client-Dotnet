using System.Reflection;
using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace EventStore.Client;

class ComparableAssertion : CompositeIdiomaticAssertion {
	public ComparableAssertion(ISpecimenBuilder builder) : base(CreateChildrenAssertions(builder)) { }

	static IEnumerable<IIdiomaticAssertion> CreateChildrenAssertions(ISpecimenBuilder builder) {
		yield return new ImplementsIComparableCorrectlyAssertion();
		yield return new SameValueComparableAssertion(builder);
		yield return new DifferentValueComparableAssertion(builder);
	}

	class ImplementsIComparableCorrectlyAssertion : IdiomaticAssertion {
		public override void Verify(Type type) =>
			Assert.False(
				type.ImplementsGenericIComparable() && !type.ImplementsIComparable(),
				$"The type {type} implemented IComparable<T> without implementing IComparable."
			);
	}

	class SameValueComparableAssertion : IdiomaticAssertion {
		readonly ISpecimenBuilder _builder;

		public SameValueComparableAssertion(ISpecimenBuilder builder) => _builder = builder;

		public override void Verify(Type type) {
			if (!type.ImplementsGenericIComparable() || !type.ImplementsIComparable())
				return;

			var context = new SpecimenContext(_builder);

			var instance = context.Resolve(type);

			Assert.True(
				type.InvokeGreaterThanOrEqualOperator(instance, instance),
				$"The type {type} did not implement >= correctly, should be true for the same instance."
			);

			Assert.False(
				type.InvokeGreaterThanOperator(instance, instance),
				$"The type {type} did not implement > correctly, should be false for the same instance."
			);

			Assert.True(
				type.InvokeLessThanOrEqualOperator(instance, instance),
				$"The type {type} did not implement <= correctly, should be true for the same instance."
			);

			Assert.False(
				type.InvokeLessThanOperator(instance, instance),
				$"The type {type} did not implement <= correctly, should be true for the same instance."
			);

			if (type.ImplementsGenericIComparable())
				Assert.Equal(0, type.InvokeGenericCompareTo(instance, instance));

			if (type.ImplementsIComparable())
				Assert.Equal(0, type.InvokeCompareTo(instance, instance));
		}
	}

	class DifferentValueComparableAssertion : IdiomaticAssertion {
		readonly ISpecimenBuilder _builder;

		public DifferentValueComparableAssertion(ISpecimenBuilder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

		public override void Verify(Type type) {
			if (!type.ImplementsGenericIComparable() || !type.ImplementsIComparable())
				return;

			var context = new SpecimenContext(_builder);

			var instance = context.Resolve(type);
			var other    = context.Resolve(type);

			var compareToGeneric = type.InvokeGenericCompareTo(instance, other);
			Assert.NotEqual(0, compareToGeneric);

			var compareTo = type.InvokeCompareTo(instance, other);
			Assert.Equal(compareToGeneric, compareTo);

			Assert.Equal(1, type.InvokeCompareTo(instance, null));

			var ex = Assert.Throws<ArgumentException>(
				() => {
					try {
						type.InvokeCompareTo(instance, new());
					}
					catch (TargetInvocationException ex) {
						throw ex.InnerException!;
					}
				}
			);

			Assert.Equal("Object is not a " + type.Name, ex.Message);

			if (compareToGeneric < 0) {
				Assert.False(
					type.InvokeGreaterThanOrEqualOperator(instance, other),
					$"The type {type} did not implement >= correctly, should be false for different instances."
				);

				Assert.False(
					type.InvokeGreaterThanOperator(instance, other),
					$"The type {type} did not implement > correctly, should be false for different instances."
				);

				Assert.True(
					type.InvokeLessThanOrEqualOperator(instance, other),
					$"The type {type} did not implement <= correctly, should be true for different instances."
				);

				Assert.True(
					type.InvokeLessThanOperator(instance, other),
					$"The type {type} did not implement <= correctly, should be true for different instances."
				);
			}
			else {
				Assert.True(
					type.InvokeGreaterThanOrEqualOperator(instance, other),
					$"The type {type} did not implement >= correctly, should be true for different instances."
				);

				Assert.True(
					type.InvokeGreaterThanOperator(instance, other),
					$"The type {type} did not implement > correctly, should be true for different instances."
				);

				Assert.False(
					type.InvokeLessThanOrEqualOperator(instance, other),
					$"The type {type} did not implement <= correctly, should be false for different instances."
				);

				Assert.False(
					type.InvokeLessThanOperator(instance, other),
					$"The type {type} did not implement <= correctly, should be false for different instances."
				);
			}
		}
	}
}