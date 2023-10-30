using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestNode : TestContainerService {
	public EventStoreTestNode(EventStoreFixtureOptions? options = null) =>
        Options =  options ?? DefaultOptions();

    EventStoreFixtureOptions Options { get; }

    static int _port = 2213;     
    
    static int NextPort() => Interlocked.Increment(ref _port);
    
	public static EventStoreFixtureOptions DefaultOptions() {
		const string connString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";
		
		var defaultSettings = EventStoreClientSettings
			.Create(connString.Replace("{port}", NextPort().ToString()))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(180))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 30)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1));

        var defaultEnvironment = new Dictionary<string, string>(GlobalEnvironment.Variables) {
	        ["EVENTSTORE_MEM_DB"]                       = "true",
	        ["EVENTSTORE_CHUNK_SIZE"]                   = (1024 * 1024).ToString(),
	        ["EVENTSTORE_CERTIFICATE_FILE"]             = "/etc/eventstore/certs/node/node.crt",
	        ["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"] = "/etc/eventstore/certs/node/node.key",
	        ["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
	        ["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000",
	        ["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]    = "True"
        }; 
        
		return new(defaultSettings, defaultEnvironment);
	}
	
	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var certsPath     = Path.Combine(Environment.CurrentDirectory, "certs");
        var containerName = $"es-dotnet-test-{Guid.NewGuid().ToString()[30..]}";

        CertificatesManager.VerifyCertificatesExist(certsPath); // its ok... no really...
        
        return new Builder()
            .UseContainer()
            .UseImage(GlobalEnvironment.DockerImage)
            .WithName(containerName)
            .WithEnvironment(env)
            .MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
            .ExposePort(Options.ClientSettings.ConnectivitySettings.Address.Port, 2113)
            .WaitForHealthy(TimeSpan.FromSeconds(60));
    }
}