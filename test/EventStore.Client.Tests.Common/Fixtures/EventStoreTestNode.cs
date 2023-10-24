using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestNode : TestContainerService {
	const string ConnectionString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";
	
	public EventStoreTestNode(EventStoreFixtureOptions? options = null) =>
        Options =  options ?? DefaultOptions();

    EventStoreFixtureOptions Options { get; }

    static int _port = 2213;     
    
    static int NextPort() => Interlocked.Increment(ref _port);
    
	public static EventStoreFixtureOptions DefaultOptions() {
		var defaultSettings = EventStoreClientSettings.Create(ConnectionString.Replace("{port}", NextPort().ToString()));

        defaultSettings.LoggerFactory   = new SerilogLoggerFactory(Log.Logger);
        defaultSettings.DefaultDeadline = new TimeSpan?(); //Debugger.IsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(30);
        
        defaultSettings.ConnectivitySettings.MaxDiscoverAttempts = 50;
        defaultSettings.ConnectivitySettings.DiscoveryInterval   = TimeSpan.FromSeconds(1);

		var defaultEnvironment = new Dictionary<string, string> {
			["EVENTSTORE_DB_LOG_FORMAT"]                  = GlobalEnvironment.DbLogFormat,
			["EVENTSTORE_MEM_DB"]                         = "true",
			["EVENTSTORE_CHUNK_SIZE"]                     = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"]               = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]   = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"] = "/etc/eventstore/certs/ca",
			["EVENTSTORE_LOG_LEVEL"]                      = "Verbose",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]   = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]     = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]      = "True",
            ["EVENTSTORE_RUN_PROJECTIONS"]                = "ALL",
            ["EVENTSTORE_START_STANDARD_PROJECTIONS"]     = "True"

            // EVENTSTORE_CLUSTER_SIZE = 4
            // EVENTSTORE_INT_TCP_PORT = 1112
            // EVENTSTORE_HTTP_PORT = 2113
            // EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH = /etc/eventstore/certs/ca
            // EVENTSTORE_DISCOVER_VIA_DNS = false
            // EVENTSTORE_ENABLE_EXTERNAL_TCP = false
            // EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP = true
            // EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE = 10000
		};
		
		return new(defaultSettings, defaultEnvironment, GlobalEnvironment.CertificateDirectory);
	}
	
	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

        var containerName = $"es-dotnet-test-{Guid.NewGuid().ToString()[30..]}";

        return new Builder()
            .UseContainer()
            .UseImage(GlobalEnvironment.DockerImage)
            .WithName(containerName)
            .WithEnvironment(env)
            .MountVolume(Options.CertificateDirectory.FullName, "/etc/eventstore/certs", MountType.ReadOnly)
            .ExposePort(Options.ClientSettings.ConnectivitySettings.Address.Port, 2113)
            .WaitForHealthy(TimeSpan.FromSeconds(60));
    }
}