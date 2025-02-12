namespace Kurrent.Client.Tests.Streams.Serialization;

static class TypeProvider {
	public static Type? GetTypeByFullName(string fullName) =>
		Type.GetType(fullName) ?? GetFirstMatchingTypeFromCurrentDomainAssembly(fullName);

	static Type? GetFirstMatchingTypeFromCurrentDomainAssembly(string fullName) {
		var firstNamespacePart = fullName.Split('.')[0];
		
		return AppDomain.CurrentDomain.GetAssemblies()
			.OrderByDescending(assembly => assembly.FullName?.StartsWith(firstNamespacePart) == true)
			.Select(assembly => assembly.GetType(fullName))
			.FirstOrDefault(type => type != null);
	}
}
