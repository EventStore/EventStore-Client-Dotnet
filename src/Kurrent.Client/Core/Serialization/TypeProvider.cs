using System.Reflection;

namespace Kurrent.Client.Tests.Streams.Serialization;

public static class TypeProvider
{
	public static Type? GetTypeFromAnyReferencingAssembly(string typeName)
	{
		var referencedAssemblies = Assembly.GetEntryAssembly()?
			.GetReferencedAssemblies()
			.Select(a => a.FullName);

		if (referencedAssemblies == null)
			return null;

		return AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => referencedAssemblies.Contains(a.FullName))
			.SelectMany(a => a.GetTypes().Where(x => x.FullName == typeName || x.Name == typeName))
			.FirstOrDefault();
	}

	public static Type? GetFirstMatchingTypeFromCurrentDomainAssembly(string typeName) =>
		AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes().Where(x => x.FullName == typeName || x.Name == typeName))
			.FirstOrDefault();
}
