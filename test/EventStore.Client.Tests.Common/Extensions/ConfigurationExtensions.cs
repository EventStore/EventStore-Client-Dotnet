using Microsoft.Extensions.Configuration;

namespace EventStore.Client.Tests;

public static class ConfigurationExtensions {
	public static void EnsureValue(this IConfiguration configuration, string key, string defaultValue) {
		var value = configuration.GetValue<string?>(key);

		if (string.IsNullOrEmpty(value))
			configuration[key] = defaultValue;
	}
}
