using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace Kurrent.Client.Tests;

public static class GlobalEnvironment {
	static GlobalEnvironment() {
		EnsureDefaults(Application.Configuration);

		UseCluster        = Application.Configuration.GetValue<bool>("ES_USE_CLUSTER");
		UseExternalServer = Application.Configuration.GetValue<bool>("ES_USE_EXTERNAL_SERVER");
		DockerImage       = Application.Configuration.GetValue<string>("ES_DOCKER_IMAGE")!;

		Variables = Application.Configuration.AsEnumerable()
			.Where(x => x.Key.StartsWith("ES_") || x.Key.StartsWith("EVENTSTORE_"))
			.OrderBy(x => x.Key)
			.ToImmutableDictionary(x => x.Key, x => x.Value ?? string.Empty)!;

		return;

		static void EnsureDefaults(IConfiguration configuration) {
			configuration.EnsureValue("ES_USE_CLUSTER", "false");
			configuration.EnsureValue("ES_USE_EXTERNAL_SERVER", "false");

			configuration.EnsureValue("ES_DOCKER_REGISTRY", "docker.eventstore.com/eventstore-ce/eventstoredb-ce");
			configuration.EnsureValue("ES_DOCKER_TAG", "ci");
			configuration.EnsureValue("ES_DOCKER_IMAGE", $"{configuration["ES_DOCKER_REGISTRY"]}:{configuration["ES_DOCKER_TAG"]}");

			configuration.EnsureValue("EVENTSTORE_TELEMETRY_OPTOUT", "true");
			configuration.EnsureValue("EVENTSTORE_ALLOW_UNKNOWN_OPTIONS", "true");
			configuration.EnsureValue("EVENTSTORE_MEM_DB", "false");
			configuration.EnsureValue("EVENTSTORE_RUN_PROJECTIONS", "None");
			configuration.EnsureValue("EVENTSTORE_START_STANDARD_PROJECTIONS", "false");
			configuration.EnsureValue("EVENTSTORE_LOG_LEVEL", "Information");
			configuration.EnsureValue("EVENTSTORE_DISABLE_LOG_FILE", "true");
			configuration.EnsureValue("EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH", "/etc/eventstore/certs/ca");
			configuration.EnsureValue("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true");
		}
	}

	public static ImmutableDictionary<string, string?> Variables { get; }

	public static bool   UseCluster        { get; }
	public static bool   UseExternalServer { get; }
	public static string DockerImage       { get; }
}
