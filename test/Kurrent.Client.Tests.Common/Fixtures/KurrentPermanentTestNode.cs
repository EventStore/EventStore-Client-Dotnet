// ReSharper disable InconsistentNaming

// using Ductus.FluentDocker.Builders;
// using Ductus.FluentDocker.Model.Builders;
// using Kurrent.Client.Tests.FluentDocker;
//
// namespace Kurrent.Client.Tests;
//
// public class EventStorePermanentTestNode(EventStoreFixtureOptions? options = null) : BaseTestNode(options) {
// 	protected override ContainerBuilder ConfigureContainer(ContainerBuilder builder) {
// 		var port      = Options.ClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;
// 		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
//
// 		var containerName = "es-client-dotnet-test";
//
// 		return builder
// 			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
// 			.WithName(containerName)
// 			.WithPublicEndpointResolver()
// 			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
// 			.ExposePort(port, 2113)
// 			.KeepContainer().KeepRunning().ReuseIfExists()
// 			.WaitUntilReadyWithConstantBackoff(
// 				1_000,
// 				60,
// 				service => {
// 					var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/eventstore/certs/ca/ca.crt https://localhost:2113/health/live");
// 					if (!output.Success)
// 						throw new Exception(output.Error);
// 				}
// 			);
// 	}
// }

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services.Extensions;
using EventStore.Client;
using Kurrent.Client;
using Kurrent.Client.Tests.FluentDocker;
using Humanizer;
using Kurrent.Client.Tests;
using Serilog;
using Serilog.Extensions.Logging;
using static System.TimeSpan;

public class KurrentPermanentTestNode(KurrentFixtureOptions? options = null) : TestContainerService {
	static readonly NetworkPortProvider NetworkPortProvider = new(NetworkPortProvider.DefaultEsdbPort);

	KurrentFixtureOptions Options { get; } = options ?? DefaultOptions();

	static Version? _version;

	public static Version Version => _version ??= GetVersion();

	public static KurrentFixtureOptions DefaultOptions() {
		const string connString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		var port = NetworkPortProvider.NextAvailablePort;

		var defaultSettings = KurrentClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : FromSeconds(30))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 20)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_MEM_DB"]                           = "true",
			["EVENTSTORE_CERTIFICATE_FILE"]                 = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]     = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]     = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]       = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]        = "true",
			["EVENTSTORE_LOG_LEVEL"]                        = "Default", // required to use serilog settings
			["EVENTSTORE_DISABLE_LOG_FILE"]                 = "true",
			["EVENTSTORE_START_STANDARD_PROJECTIONS"]       = "true",
			["EVENTSTORE_RUN_PROJECTIONS"]                  = "All",
			["EVENTSTORE_CHUNK_SIZE"]                       = (1024 * 1024 * 1024).ToString(),
			["EVENTSTORE_MAX_APPEND_SIZE"]                  = 100.Kilobytes().Bytes.ToString(CultureInfo.InvariantCulture),
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{NetworkPortProvider.DefaultEsdbPort}"
		};

		if (port != NetworkPortProvider.DefaultEsdbPort) {
			if (GlobalEnvironment.Variables.TryGetValue("ES_DOCKER_TAG", out var tag) && tag == "ci")
				defaultEnvironment["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = $"{port}";
			else
				defaultEnvironment["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{port}";
		}

		return new(defaultSettings, defaultEnvironment);
	}

	static Version GetVersion() {
		const string versionPrefix = "EventStoreDB version";

		using var cts = new CancellationTokenSource(FromSeconds(30));
		using var eventstore = new Builder().UseContainer()
			.UseImage(GlobalEnvironment.DockerImage)
			.Command("--version")
			.Build()
			.Start();

		using var log = eventstore.Logs(true, cts.Token);
		foreach (var line in log.ReadToEnd()) {
			if (line.StartsWith(versionPrefix) &&
			    Version.TryParse(new string(ReadVersion(line[(versionPrefix.Length + 1)..]).ToArray()), out var version)) {
				return version;
			}
		}

		throw new InvalidOperationException("Could not determine server version.");

		IEnumerable<char> ReadVersion(string s) {
			foreach (var c in s.TakeWhile(c => c == '.' || char.IsDigit(c))) {
				yield return c;
			}
		}
	}

	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var port      = Options.ClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;
		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

		var containerName = port == 2113
			? "es-client-dotnet-test"
			: $"es-client-dotnet-test-{port}-{Guid.NewGuid().ToString()[30..]}";

		CertificatesManager.VerifyCertificatesExist(certsPath);

		return new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName(containerName)
			.WithPublicEndpointResolver()
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
			.KeepContainer().KeepRunning().ReuseIfExists()
			.WaitUntilReadyWithConstantBackoff(
				1_000,
				60,
				service => {
					var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/eventstore/certs/ca/ca.crt https://localhost:2113/health/live");
					if (!output.Success)
						throw new Exception(output.Error);
				}
			);
	}
}

/// <summary>
/// Using the default 2113 port assumes that the test is running sequentially.
/// </summary>
/// <param name="port"></param>
class NetworkPortProvider(int port = 2114) {
	public const int DefaultEsdbPort = 2113;

	static readonly SemaphoreSlim Semaphore = new(1, 1);

	async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
		// TODO SS: find a way to enable parallel tests on CI
		if (port == DefaultEsdbPort)
			return port;

		await Semaphore.WaitAsync();

		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			while (true) {
				var nexPort = Interlocked.Increment(ref port);

				try {
					await socket.ConnectAsync(IPAddress.Any, nexPort);
				} catch (SocketException ex) {
					if (ex.SocketErrorCode is SocketError.ConnectionRefused or not SocketError.IsConnected) {
						return nexPort;
					}

					await Task.Delay(delay);
				} finally {
#if NET
					if (socket.Connected) await socket.DisconnectAsync(true);
#else
					if (socket.Connected) socket.Disconnect(true);
#endif
				}
			}
		} finally {
			Semaphore.Release();
		}
	}

	public int NextAvailablePort => GetNextAvailablePort(FromMilliseconds(100)).GetAwaiter().GetResult();
}
