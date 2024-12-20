using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace EventStore.Client.Tests;

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
			configuration.EnsureValue("ES_DOCKER_TAG", "previous-lts");
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

	#region . Obsolete .

	//[Obsolete("Use the EventStoreFixture instead so you don't have to use this method.", false)]
	public static IDictionary<string, string> GetEnvironmentVariables(IDictionary<string, string>? overrides = null) {
		var env = new Dictionary<string, string> {
			["ES_DOCKER_TAG"]            = "ci",
			["EVENTSTORE_DB_LOG_FORMAT"] = "V2",
		};

		foreach (var @override in overrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
			if (@override.Key.StartsWith("EVENTSTORE") && !SharedEnv.Contains(@override.Key))
				throw new Exception($"Add {@override.Key} to shared.env and _sharedEnv to pass it to the cluster containers");

			env[@override.Key] = @override.Value;
		}

		return env;
	}

	// matches with the pass-through vars in shared.env... better way?
	static readonly HashSet<string> SharedEnv = new() {
		"EVENTSTORE_DB_LOG_FORMAT",
		"EVENTSTORE_LOG_LEVEL",
		"EVENTSTORE_MAX_APPEND_SIZE",
		"EVENTSTORE_MEM_DB",
		"EVENTSTORE_RUN_PROJECTIONS",
		"EVENTSTORE_START_STANDARD_PROJECTIONS",
	};

	#endregion
}
