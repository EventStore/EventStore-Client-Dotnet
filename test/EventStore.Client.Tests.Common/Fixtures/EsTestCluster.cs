using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ductus.FluentDocker.Builders;
using EventStore.Client;
using Serilog.Extensions.Logging;

namespace EventStore.Tests.Fixtures;

public class EsTestCluster : TestCompositeContainer {
	const string ConnectionString = "esdb://admin:changeit@localhost:2113,localhost:2112,localhost:2111?tls=true&tlsVerifyCert=false";

	public EsTestCluster(EsTestDbOptions? options = null) => Options = options ?? DefaultOptions();

	protected EsTestDbOptions Options { get; }

	public static EsTestDbOptions DefaultOptions() {
		var defaultSettings = EventStoreClientSettings.Create(ConnectionString);

		defaultSettings.LoggerFactory = new SerilogLoggerFactory();
		defaultSettings.DefaultDeadline = Debugger.IsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(30);
		defaultSettings.ConnectivitySettings.MaxDiscoverAttempts = 20;
		defaultSettings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1);
		
		var defaultEnvironment = GlobalEnvironment.EnvironmentVariables(new Dictionary<string, string> {
			["ES_CERTS_CLUSTER"] = GlobalEnvironment.HostCertificateDirectory.FullName
		});

		return new(defaultSettings, defaultEnvironment, GlobalEnvironment.HostCertificateDirectory);
	}
	
	protected override CompositeBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();
		
		return new Builder()
			.UseContainer()
			.UseCompose()
			.WithEnvironment(env)
			.FromFile("docker-compose.yml")
			.ForceRecreate()
			.RemoveOrphans();
	}
}