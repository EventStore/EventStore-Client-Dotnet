using System.Reflection;
using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace EventStore.Client;

class NullArgumentAssertion : IdiomaticAssertion {
	readonly ISpecimenBuilder _builder;

	public NullArgumentAssertion(ISpecimenBuilder builder) => _builder = builder;

	public override void Verify(Type type) {
		var context = new SpecimenContext(_builder);

		Assert.All(
			type.GetConstructors(),
			constructor => {
				var parameters = constructor.GetParameters();

				Assert.All(
					parameters.Where(
						p => p.ParameterType.IsClass ||
						     p.ParameterType == typeof(string) ||
						     (p.ParameterType.IsGenericType &&
						      p.ParameterType.GetGenericArguments().FirstOrDefault() ==
						      typeof(Nullable<>))
					),
					p => {
						var args = new object[parameters.Length];

						for (var i = 0; i < args.Length; i++)
							if (i != p.Position)
								args[i] = context.Resolve(p.ParameterType);

						var ex = Assert.Throws<ArgumentNullException>(
							() => {
								try {
									constructor.Invoke(args);
								}
								catch (TargetInvocationException ex) {
									throw ex.InnerException!;
								}
							}
						);

						Assert.Equal(p.Name, ex.ParamName);
					}
				);
			}
		);
	}
}
