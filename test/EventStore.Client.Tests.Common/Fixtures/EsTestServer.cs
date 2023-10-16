using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using EventStore.Client;
using Serilog.Extensions.Logging;

namespace EventStore.Tests.Fixtures;

public class EsTestServer : TestContainer {
	const string ConnectionString = "esdb://admin:changeit@localhost:2113/?tlsVerifyCert=false";
	
	public EsTestServer(EsTestDbOptions? options = null) => Options =  options ?? DefaultOptions();

	protected EsTestDbOptions Options { get; }

	public static EsTestDbOptions DefaultOptions() {
		var defaultSettings = EventStoreClientSettings.Create(ConnectionString);

		defaultSettings.LoggerFactory = new SerilogLoggerFactory();
		defaultSettings.DefaultDeadline = Debugger.IsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(30);
		defaultSettings.ConnectivitySettings.MaxDiscoverAttempts = 20;
		defaultSettings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1);

		var defaultEnvironment = new Dictionary<string, string> {
			["EVENTSTORE_DB_LOG_FORMAT"] = GlobalEnvironment.DbLogFormat,
			["EVENTSTORE_MEM_DB"] = "true",
			["EVENTSTORE_CHUNK_SIZE"] = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"] = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"] = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"] = "/etc/eventstore/certs/ca",
			["EVENTSTORE_LOG_LEVEL"] = "Verbose",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"] = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"] = "True"
		};
		
		return new(defaultSettings, defaultEnvironment, GlobalEnvironment.HostCertificateDirectory);
	}
	
	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		return new Builder()
			.UseContainer()
			.UseImage(GlobalEnvironment.DockerImage)
			.WithName("es-dotnet-test")
			.WithEnvironment(env)
			.MountVolume(Options.CertificateDirectory.FullName, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(2113, 2113)
			.WaitForHealthy(TimeSpan.FromSeconds(30))
			.ReuseIfExists();
	}
}