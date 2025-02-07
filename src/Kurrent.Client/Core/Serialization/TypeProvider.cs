using System.Reflection;

namespace Kurrent.Client.Tests.Streams.Serialization;

static class TypeProvider {
	public static Type? GetTypeWithAutoLoad(string typeName) =>
		Type.GetType(
			typeName,
			assemblyResolver: Assembly.Load,
			typeResolver: null
		);
}
