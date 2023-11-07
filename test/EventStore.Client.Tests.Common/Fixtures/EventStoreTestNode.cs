using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using EventStore.Client.Tests.FluentDocker;
using Polly;
using Serilog;
using Serilog.Extensions.Logging;

namespace EventStore.Client.Tests;

public class EventStoreTestNode(EventStoreFixtureOptions? options = null) : TestContainerService {
	static readonly NetworkPortProvider NetworkPortProvider = new();

	EventStoreFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static EventStoreFixtureOptions DefaultOptions() {
		const string connString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		// disable it for now
		//var port = $"{NetworkPortProvider.NextAvailablePort}";

		var port = "2113";

		var defaultSettings = EventStoreClientSettings
			.Create(connString.Replace("{port}", port))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : TimeSpan.FromSeconds(30))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 20)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			//["EVENTSTORE_NODE_PORT"]                        = port,
			// ["EVENTSTORE_NODE_PORT_ADVERTISE_AS"] = port,
			// ["EVENTSTORE_NODE_PORT_ADVERTISE_AS"] = port,
			//["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = port,
			//["EVENTSTORE_HTTP_PORT"] = port,
			["EVENTSTORE_ENABLE_EXTERNAL_TCP"]          = "false",
			["EVENTSTORE_MEM_DB"]                       = "true",
			["EVENTSTORE_CHUNK_SIZE"]                   = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"]             = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"] = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]    = "false",
			["EVENTSTORE_DISABLE_LOG_FILE"]             = "true"
		};

		return new(defaultSettings, defaultEnvironment);
	}

	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var port      = Options.ClientSettings.ConnectivitySettings.Address.Port;
		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
		//var containerName = $"esbd-dotnet-test-{port}-{Guid.NewGuid().ToString()[30..]}";
		var containerName = "es-client-dotnet-test";

		CertificatesManager.VerifyCertificatesExist(certsPath); // its ok... no really...

		return new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName(containerName)
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
			.WaitForMessageInLog("'admin' user added to $users.", TimeSpan.FromSeconds(60));
		//.Wait("", (svc, count) => CheckConnection());
		// .KeepContainer()
		// .WaitForHealthy(TimeSpan.FromSeconds(60));
		//.WaitForMessageInLog($"========== [\"0.0.0.0:{port}\"] IS LEADER... SPARTA!")
		//.WaitForMessageInLog("'ops' user added to $users.")
		//.WaitForMessageInLog("'admin' user added to $users.");
	}

	protected override async Task OnServiceStarted() {
		using var http = new HttpClient(
			new SocketsHttpHandler {
				SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
			}
		) {
			BaseAddress = Options.ClientSettings.ConnectivitySettings.Address
		};

		await Policy.Handle<Exception>()
			.WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100))
			.ExecuteAsync(
				async () => {
					using var response = await http.GetAsync("/health/live", CancellationToken.None);
					if (response.StatusCode >= HttpStatusCode.BadRequest)
						throw new FluentDockerException($"Health check failed with status code: {response.StatusCode}.");
				}
			);
	}
}

class NetworkPortProvider(int port = 2212) {
	static readonly SemaphoreSlim Semaphore = new(1, 1);

	public async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
		await Semaphore.WaitAsync();

		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			while (true) {
				var nexPort = Interlocked.Increment(ref port);

				try {
					await socket.ConnectAsync(IPAddress.Any, nexPort);
				}
				catch (SocketException ex) {
					if (ex.SocketErrorCode is SocketError.ConnectionRefused or not SocketError.IsConnected) {
						return nexPort;
					}

					await Task.Delay(delay);
				}
				finally {
					if (socket.Connected) {
						await socket.DisconnectAsync(true);
					}
				}
			}
		}
		finally {
			Semaphore.Release();
		}
	}

	public int NextAvailablePort => GetNextAvailablePort(TimeSpan.FromMilliseconds(100)).GetAwaiter().GetResult();
}