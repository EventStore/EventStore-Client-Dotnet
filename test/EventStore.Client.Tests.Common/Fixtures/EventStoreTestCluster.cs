using Ductus.FluentDocker.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestCluster(EventStoreFixtureOptions options) : TestCompositeService {
	EventStoreFixtureOptions Options { get; } = options;

	public static EventStoreFixtureOptions DefaultOptions() {
		const string connString = "esdb://localhost:2113,localhost:2112,localhost:2111?tls=true&tlsVerifyCert=false";

		var defaultSettings = EventStoreClientSettings
			.Create(connString)
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(30))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 30)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["ES_CERTS_CLUSTER"]                        = Path.Combine(Environment.CurrentDirectory, "certs-cluster"),
			["EVENTSTORE_CLUSTER_SIZE"]                 = "3",
			["EVENTSTORE_INT_TCP_PORT"]                 = "1112",
			["EVENTSTORE_HTTP_PORT"]                    = "2113",
			["EVENTSTORE_DISCOVER_VIA_DNS"]             = "false",
			["EVENTSTORE_ENABLE_EXTERNAL_TCP"]          = "false",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000"
		};

		return new(defaultSettings, defaultEnvironment);
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