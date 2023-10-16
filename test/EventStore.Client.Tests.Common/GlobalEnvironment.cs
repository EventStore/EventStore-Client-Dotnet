using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EventStore.Client {
	public static class GlobalEnvironment {
		static string UseClusterName => "ES_USE_CLUSTER";
		static string UseExternalServerName => "ES_USE_EXTERNAL_SERVER";
		static string ContainerRegistry => "ghcr.io/eventstore/eventstore";
		static string ImageTagName => "ES_DOCKER_TAG";
		static string ImageTagDefault => "ci"; // e.g. "21.10.1-focal";
		static string DbLogFormatName => "EVENTSTORE_DB_LOG_FORMAT";
		static string DbLogFormatDefault => "V2";
		
		static GlobalEnvironment() {
			var useClusterEnvVar = Environment.GetEnvironmentVariable(UseClusterName);
			if (bool.TryParse(useClusterEnvVar, out var useCluster)) {
				UseCluster = useCluster;
			}

			var useExternalServerEnvVar = Environment.GetEnvironmentVariable(UseExternalServerName);
			if (bool.TryParse(useExternalServerEnvVar, out var useExternalServer)) {
				UseExternalServer = useExternalServer;
			}

			HostCertificateDirectory = new(Path.Combine(ProjectDir.Current, "..", "..", UseCluster ? "certs-cluster" : "certs"));
		}

		public static DirectoryInfo HostCertificateDirectory { get; }

		public static bool UseCluster { get; }
		public static bool UseExternalServer { get; }
		
		public static string DockerImage => $"{ContainerRegistry}:{ImageTag}";
		public static string ImageTag => GetEnvironmentVariable(ImageTagName, ImageTagDefault);
		public static string DbLogFormat => GetEnvironmentVariable(DbLogFormatName, DbLogFormatDefault);

		public static IDictionary<string, string> EnvironmentVariables(IDictionary<string, string>? overrides = null) {
			var env = new Dictionary<string, string> {
				[ImageTagName] = ImageTag,
				[DbLogFormatName] = DbLogFormat,
			};

			foreach (var (key, value) in overrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
				if (key.StartsWith("EVENTSTORE") && !_sharedEnv.Contains(key))
					throw new Exception($"Add {key} to shared.env and _sharedEnv to pass it to the cluster containers");
				env[key] = value;
			}
			return env;
		}

		// matches with the pass-through vars in shared.env... better way?
		static readonly HashSet<string> _sharedEnv = new HashSet<string>() {
			"EVENTSTORE_DB_LOG_FORMAT",
			"EVENTSTORE_LOG_LEVEL",
			"EVENTSTORE_MAX_APPEND_SIZE",
			"EVENTSTORE_MEM_DB",
			"EVENTSTORE_RUN_PROJECTIONS",
			"EVENTSTORE_START_STANDARD_PROJECTIONS",
		};

		static string GetEnvironmentVariable(string name, string def) {
			var x = Environment.GetEnvironmentVariable(name);
			return string.IsNullOrWhiteSpace(x) ? def : x;
		}
	}
}
