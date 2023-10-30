using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace EventStore.Client.Tests;

public static class GlobalEnvironment {
	static GlobalEnvironment() {
		Variables = FromConfiguration(Application.Configuration);

		UseCluster        = false; // Application.Configuration.GetValue("ES_USE_CLUSTER", false);
		UseExternalServer = Application.Configuration.GetValue("ES_USE_EXTERNAL_SERVER", false);
		DockerImage       = Variables["ES_DOCKER_IMAGE"];
		DbLogFormat       = Variables["EVENTSTORE_DB_LOG_FORMAT"];
	}

	public static ImmutableDictionary<string, string> Variables { get; }

	public static bool   UseCluster        { get; }
	public static bool   UseExternalServer { get; }
	public static string DockerImage       { get; }
	public static string DbLogFormat       { get; }
	
	public static ImmutableDictionary<string, string> FromConfiguration(IConfiguration configuration) {
		var env = configuration.AsEnumerable()
			.Where(x => x.Key.StartsWith("ES_") || x.Key.StartsWith("EVENTSTORE_"))
			.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);

		EnsureSet(env, "ES_USE_CLUSTER", "false");
		EnsureSet(env, "ES_USE_EXTERNAL_SERVER", "false");
		
		EnsureSet(env, "ES_DOCKER_REGISTRY", "ghcr.io/eventstore/eventstore");
		EnsureSet(env, "ES_DOCKER_TAG", "ci");
		EnsureSet(env, "ES_DOCKER_IMAGE", $"{env["ES_DOCKER_REGISTRY"]}:{env["ES_DOCKER_TAG"]}");
		
		EnsureSet(env, "EVENTSTORE_MEM_DB", "false");
		EnsureSet(env, "EVENTSTORE_RUN_PROJECTIONS", "None");
		EnsureSet(env, "EVENTSTORE_START_STANDARD_PROJECTIONS", "false");
		EnsureSet(env, "EVENTSTORE_DB_LOG_FORMAT", "V2");
		EnsureSet(env, "EVENTSTORE_LOG_LEVEL", "Verbose");
		EnsureSet(env, "EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH", "/etc/eventstore/certs/ca");
		
		return env.ToImmutableDictionary();
		
		static void EnsureSet(IDictionary<string, string> dic, string key, string value) {
			if (!dic.TryGetValue(key, out var actualValue) || string.IsNullOrEmpty(actualValue))
				dic[key] = value;
		}
	}
	
	#region . Obsolete .

	//[Obsolete("Use the EventStoreFixture instead so you don't have to use this method.", false)]
	public static IDictionary<string, string> GetEnvironmentVariables(IDictionary<string, string>? overrides = null) {
		var env = new Dictionary<string, string> {
			["ES_DOCKER_TAG"]            = "ci",
			["EVENTSTORE_DB_LOG_FORMAT"] = "V2",
		};

		foreach (var (key, value) in overrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
			if (key.StartsWith("EVENTSTORE") && !SharedEnv.Contains(key))
				throw new Exception($"Add {key} to shared.env and _sharedEnv to pass it to the cluster containers");

			env[key] = value;
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