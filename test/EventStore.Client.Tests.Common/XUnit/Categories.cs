// ReSharper disable CheckNamespace

using Xunit.Sdk;

namespace Xunit.Categories;

class TraitDiscoverer {
	internal const string AssemblyName = "Xunit.Categories";
}

[PublicAPI]
[TraitDiscoverer(DedicatedDatabaseDiscoverer.DiscovererTypeName, TraitDiscoverer.AssemblyName)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class DedicatedDatabaseAttribute : Attribute, ITraitAttribute {
	public class DedicatedDatabaseDiscoverer : ITraitDiscoverer {
		internal const string DiscovererTypeName = $"{TraitDiscoverer.AssemblyName}.{nameof(DedicatedDatabaseAttribute)}{nameof(DedicatedDatabaseDiscoverer)}";

		public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) {
			yield return new("Category", "DedicatedDatabase");
		}
	}
}

[PublicAPI]
[TraitDiscoverer(LongRunningDiscoverer.DiscovererTypeName, TraitDiscoverer.AssemblyName)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class LongRunningAttribute : Attribute, ITraitAttribute {
	public class LongRunningDiscoverer : ITraitDiscoverer {
		internal const string DiscovererTypeName = $"{TraitDiscoverer.AssemblyName}.{nameof(LongRunningAttribute)}{nameof(LongRunningDiscoverer)}";

		public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) {
			yield return new("Category", "LongRunning");
		}
	}
}

[PublicAPI]
[TraitDiscoverer(NetworkDiscoverer.DiscovererTypeName, TraitDiscoverer.AssemblyName)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class NetworkAttribute : Attribute, ITraitAttribute {
	public class NetworkDiscoverer : ITraitDiscoverer {
		internal const string DiscovererTypeName = $"{TraitDiscoverer.AssemblyName}.{nameof(NetworkAttribute)}{nameof(NetworkDiscoverer)}";

		public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) {
			yield return new("Category", "Network");
		}
	}
}

