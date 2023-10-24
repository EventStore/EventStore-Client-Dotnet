using Ductus.FluentDocker.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestCluster : TestCompositeService {
	const string ConnectionString = "esdb://localhost:2113,localhost:2112,localhost:2111?tls=true&tlsVerifyCert=false";

	public EventStoreTestCluster(EventStoreFixtureOptions? options = null) =>
        Options = options ?? DefaultOptions();

    EventStoreFixtureOptions Options { get; }

	public static EventStoreFixtureOptions DefaultOptions() {
		var defaultSettings = EventStoreClientSettings.Create(ConnectionString);

		defaultSettings.LoggerFactory                            = new SerilogLoggerFactory();
        defaultSettings.DefaultDeadline                          = new TimeSpan?(); //Debugger.IsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(180);
		defaultSettings.ConnectivitySettings.MaxDiscoverAttempts = 20;
		defaultSettings.ConnectivitySettings.DiscoveryInterval   = TimeSpan.FromSeconds(1);
        
        // ES_CERTS_CLUSTER = ./certs-cluster
        
		var defaultEnvironment = GlobalEnvironment.GetEnvironmentVariables(
            new Dictionary<string, string> {
                ["ES_CERTS_CLUSTER"] = GlobalEnvironment.CertificateDirectory.FullName,
                ["ES_DOCKER_TAG"] = "ci", 
                //["ES_DOCKER_TAG"] = "latest"
                ["EVENTSTORE_MEM_DB"] = "false",
                ["EVENTSTORE_RUN_PROJECTIONS"]            = "ALL",
                ["EVENTSTORE_START_STANDARD_PROJECTIONS"] = "True"
            }
        );

		return new(defaultSettings, defaultEnvironment, GlobalEnvironment.CertificateDirectory);
	}
	
	protected override CompositeBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

        var builder = new Builder()
            .UseContainer()
            .FromComposeFile("docker-compose.yml")
            .ServiceName("esdb-test-cluster")
            .WithEnvironment(env)
            .RemoveOrphans()
            .NoRecreate()
            .KeepRunning();
        
        return builder;
    }

    protected override async Task OnServiceStarted() {
        await Service.WaitUntilNodesAreHealthy("esdb-node", TimeSpan.FromSeconds(60));
    }
}