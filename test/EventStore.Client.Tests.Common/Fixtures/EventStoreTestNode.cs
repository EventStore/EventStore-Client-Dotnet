using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Builders;
using EventStore.Client.Tests.FluentDocker;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using Serilog.Extensions.Logging;
using static System.TimeSpan;

namespace EventStore.Client.Tests;

public class EventStoreTestNode(EventStoreFixtureOptions? options = null) : TestContainerService {
	
	static readonly NetworkPortProvider NetworkPortProvider = new(NetworkPortProvider.DefaultEsdbPort);
	
	EventStoreFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static EventStoreFixtureOptions DefaultOptions() {
		const string connString = "esdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";
		
		var port = NetworkPortProvider.NextAvailablePort;
		
		var defaultSettings = EventStoreClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? new TimeSpan?() : FromSeconds(30))
			.With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 20)
			.With(x => x.ConnectivitySettings.DiscoveryInterval = FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_ENABLE_EXTERNAL_TCP"]          = "false",
			["EVENTSTORE_MEM_DB"]                       = "true",
			["EVENTSTORE_CHUNK_SIZE"]                   = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"]             = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"] = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]    = "true",
			["EVENTSTORE_DISABLE_LOG_FILE"]             = "true"
		};
		
		// TODO SS: must find a way to enable parallel tests on CI. It works locally.
		if (port != NetworkPortProvider.DefaultEsdbPort) {
			if (GlobalEnvironment.Variables.TryGetValue("ES_DOCKER_TAG", out var tag) && tag == "ci")
				defaultEnvironment["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = $"{port}";
			else
				defaultEnvironment["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{port}";
		}
		
		return new(defaultSettings, defaultEnvironment);
	}

	protected override ContainerBuilder Configure() {
		var env = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var port      = Options.ClientSettings.ConnectivitySettings.Address.Port;
		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

		var containerName = port == 2113
			? "es-client-dotnet-test"
			: $"es-client-dotnet-test-{port}-{Guid.NewGuid().ToString()[30..]}";

		CertificatesManager.VerifyCertificatesExist(certsPath);

		return new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName(containerName)
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113);
		//.WaitForMessageInLog("'admin' user added to $users.", FromSeconds(60));
	}

	/// <summary>
	/// max of 30 seconds (300 * 100ms)
	/// </summary>
	static readonly IEnumerable<TimeSpan> DefaultBackoffDelay = Backoff.ConstantBackoff(FromMilliseconds(100), 300);

	protected override async Task OnServiceStarted() {
		using var http = new HttpClient(
			new SocketsHttpHandler { SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } } }
		) {
			BaseAddress = Options.ClientSettings.ConnectivitySettings.Address
		};

		await Policy.Handle<Exception>()
			.WaitAndRetryAsync(DefaultBackoffDelay)
			.ExecuteAsync(
				async () => {
					using var response = await http.GetAsync("/health/live", CancellationToken.None);
					if (response.StatusCode >= HttpStatusCode.BadRequest)
						throw new FluentDockerException($"Health check failed with status code: {response.StatusCode}.");
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

	public async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
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

	public int NextAvailablePort => GetNextAvailablePort(FromMilliseconds(100)).GetAwaiter().GetResult();
}