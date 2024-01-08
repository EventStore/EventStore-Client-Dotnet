using System.Reflection;
using AutoFixture.Idioms;
using AutoFixture.Kernel;

// ReSharper disable once CheckNamespace
namespace EventStore.Client;

class StringConversionAssertion : IdiomaticAssertion {
	readonly ISpecimenBuilder _builder;

	public StringConversionAssertion(ISpecimenBuilder builder) => _builder = builder;

	public override void Verify(Type type) {
		var context = new SpecimenContext(_builder);

		var constructor = type.GetConstructor(new[] { typeof(string) });

		if (constructor is null)
			return;

		var value    = (string)context.Resolve(typeof(string));
		var instance = constructor.Invoke(new object[] { value });
		var args     = new[] { instance };

		var @explicit = type
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "op_Explicit" && m.ReturnType == typeof(string));

		if (@explicit is not null)
			Assert.Equal(value, @explicit.Invoke(null, args));

		var @implicit = type
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == typeof(string));

		if (@implicit is not null)
			Assert.Equal(value, @implicit.Invoke(null, args));

		var toString = type
			.GetMethods(BindingFlags.Public | BindingFlags.Public)
			.FirstOrDefault(m => m.Name == "ToString" && m.ReturnType == typeof(string));

		if (toString is not null)
			Assert.Equal(value, toString.Invoke(instance, null));
	}
}