namespace EventStore.Client.Tests; 

public static class GlobalEnvironment {
    static string UseClusterEnvKey        => "ES_USE_CLUSTER";
    static string UseExternalServerEnvKey => "ES_USE_EXTERNAL_SERVER";
    static string ImageTagEnvKey          => "ES_DOCKER_TAG";
    static string DbLogFormatEvnKey       => "EVENTSTORE_DB_LOG_FORMAT";
    static string DbLogFormatDefault      => "V2";
    static string ContainerRegistry       => "ghcr.io/eventstore/eventstore";
    static string ImageTagDefault         => "ci"; // e.g. "21.10.1-focal";
    
    // matches with the pass-through vars in shared.env... better way?
    static readonly HashSet<string> SharedEnv = new HashSet<string>() {
        "EVENTSTORE_DB_LOG_FORMAT",
        "EVENTSTORE_LOG_LEVEL",
        "EVENTSTORE_MAX_APPEND_SIZE",
        "EVENTSTORE_MEM_DB",
        "EVENTSTORE_RUN_PROJECTIONS",
        "EVENTSTORE_START_STANDARD_PROJECTIONS",
    };
    
    static GlobalEnvironment() {
        var useClusterEnvVar = Environment.GetEnvironmentVariable(UseClusterEnvKey);
        if (bool.TryParse(useClusterEnvVar, out var useCluster)) {
            UseCluster = useCluster;
        }
        
        var useExternalServerEnvVar = Environment.GetEnvironmentVariable(UseExternalServerEnvKey);
        if (bool.TryParse(useExternalServerEnvVar, out var useExternalServer)) {
            UseExternalServer = useExternalServer;
        }

        CertificateDirectory = new(Path.Combine(ProjectDir.Current, "..", "..", UseCluster ? "certs-cluster" : "certs"));

        ImageTag    = GetEnvironmentVariable(ImageTagEnvKey, ImageTagDefault);
        DockerImage = $"{ContainerRegistry}:{ImageTag}";
        DbLogFormat = GetEnvironmentVariable(DbLogFormatEvnKey, DbLogFormatDefault);
        
        return;

        static string GetEnvironmentVariable(string key, string defaultValue) => 
            Environment.GetEnvironmentVariable(key).WithResult(x => string.IsNullOrWhiteSpace(x) ? defaultValue : x);
    }

    public static DirectoryInfo CertificateDirectory { get; }
    public static bool          UseCluster           { get; }
    public static bool          UseExternalServer    { get; }
    public static string        DockerImage          { get; }
    public static string        ImageTag             { get; }
    public static string        DbLogFormat          { get; }

    public static IDictionary<string, string> GetEnvironmentVariables(IDictionary<string, string>? overrides = null) {
        var env = new Dictionary<string, string> {
            [ImageTagEnvKey]    = ImageTag,
            [DbLogFormatEvnKey] = DbLogFormat,
        };

        foreach (var (key, value) in overrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
            if (key.StartsWith("EVENTSTORE") && !SharedEnv.Contains(key))
                throw new Exception($"Add {key} to shared.env and _sharedEnv to pass it to the cluster containers");
            env[key] = value;
        }
        
        return env;
    }
    //
    // public static string[] GetEnvironment(IDictionary<string, string>? overrides = null) =>
    //     GetEnvironmentVariables(overrides)
    //         .Select(pair => $"{pair.Key}={pair.Value}")
    //         .ToArray();
}