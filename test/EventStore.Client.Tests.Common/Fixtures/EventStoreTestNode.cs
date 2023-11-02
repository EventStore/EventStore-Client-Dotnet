using System.Net;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestNode(EventStoreFixtureOptions? options = null) : TestContainerService {
	EventStoreFixtureOptions Options { get; } = options ?? DefaultOptions();

	static int _port = 2212;
	static int NextPort() => Interlocked.Increment(ref _port);
    
	public static EventStoreFixtureOptions DefaultOptions() {
		const string connString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		var port = NextPort().ToString();
		
		var defaultSettings = EventStoreClientSettings
			.Create(connString.Replace("{port}", port))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(180))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 30)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			// ["EVENTSTORE_HTTP_PORT"]                        = port,
			// ["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = port,
			["EVENTSTORE_NODE_PORT"]                        = port,
			["EVENTSTORE_NODE_PORT_ADVERTISE_AS"]           = port,
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = port,
			["EVENTSTORE_ENABLE_EXTERNAL_TCP"]              = "false",
			["EVENTSTORE_MEM_DB"]                           = "true",
			["EVENTSTORE_CHUNK_SIZE"]                       = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"]                 = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]     = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]     = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]       = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]        = "false",
			["EVENTSTORE_DISABLE_LOG_FILE"]                 = "true"
		}; 
        
		return new(defaultSettings, defaultEnvironment);
	}
	
	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var port          = Options.ClientSettings.ConnectivitySettings.Address.Port;
		var certsPath     = Path.Combine(Environment.CurrentDirectory, "certs");
        var containerName = $"esbd-dotnet-test-{port}-{Guid.NewGuid().ToString()[30..]}";

        CertificatesManager.VerifyCertificatesExist(certsPath); // its ok... no really...

        return new Builder()
	        .UseContainer()
	        .UseImage(Options.Environment["ES_DOCKER_IMAGE"])
	        .WithName(containerName)
	        .WithEnvironment(env)
	        .MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
	        .ExposePort(port, port) // 2113 
	        //.WaitForMessageInLog($"========== [\"0.0.0.0:{port}\"] IS LEADER... SPARTA!")
	        //.WaitForMessageInLog("'ops' user added to $users.")
	        .WaitForMessageInLog("'admin' user added to $users.");
			//.WaitForHealthy(TimeSpan.FromSeconds(60));
			//HEALTHCHECK &{["CMD-SHELL" "curl --fail --insecure https://localhost:2113/health/live || curl --fail http://localhost:2113/health/live || exit 1"] "5s" "5s" "0s" '\x18'}

	}
}