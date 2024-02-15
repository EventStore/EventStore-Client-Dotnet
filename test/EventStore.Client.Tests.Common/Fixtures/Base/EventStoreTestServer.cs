using System.Net;
using System.Net.Http;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Polly;

namespace EventStore.Client.Tests;

public class EventStoreTestServer : IEventStoreTestServer {
	static readonly string ContainerName = "es-client-dotnet-test";

	static   Version?          _version;
	readonly IContainerService _eventStore;
	readonly string            _hostCertificatePath;
	readonly HttpClient        _httpClient;

	public EventStoreTestServer(
		string hostCertificatePath,
		Uri address,
		IDictionary<string, string>? envOverrides
	) {
		_hostCertificatePath = hostCertificatePath;
		VerifyCertificatesExist();

#if NET
		_httpClient = new HttpClient(new SocketsHttpHandler {
			SslOptions = {RemoteCertificateValidationCallback = delegate { return true; }}
		}) {
			BaseAddress = address,
		};
#else
		_httpClient = new HttpClient(new WinHttpHandler {
			ServerCertificateValidationCallback = delegate { return true; }
		}) {
			BaseAddress = address,
		};
#endif

		var env = new Dictionary<string, string> {
			["EVENTSTORE_DB_LOG_FORMAT"]                  = "V2",
			["EVENTSTORE_MEM_DB"]                         = "true",
			["EVENTSTORE_CHUNK_SIZE"]                     = (1024 * 1024).ToString(),
			["EVENTSTORE_CERTIFICATE_FILE"]               = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]   = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"] = "/etc/eventstore/certs/ca",
			["EVENTSTORE_LOG_LEVEL"]                      = "Verbose",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]   = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]     = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]      = "false",
			["EVENTSTORE_DISABLE_LOG_FILE"]               = "true"
		};

		foreach (var val in envOverrides ?? Enumerable.Empty<KeyValuePair<string, string>>())
			env[val.Key] = val.Value;

		_eventStore = new Builder()
			.UseContainer()
			.UseImage(GlobalEnvironment.DockerImage)
			.WithEnvironment(env.Select(pair => $"{pair.Key}={pair.Value}").ToArray())
			.WithName(ContainerName)
			.MountVolume(_hostCertificatePath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(2113, 2113)
			//.WaitForHealthy(TimeSpan.FromSeconds(120))
			//.KeepContainer()
			//.KeepRunning()
			.Build();
	}

	public static Version Version => _version ??= GetVersion();

	public async Task StartAsync(CancellationToken cancellationToken = default) {
		_eventStore.Start();
		try {
			await Policy.Handle<Exception>()
				.WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100))
				.ExecuteAsync(
					async () => {
						using var response = await _httpClient.GetAsync("/health/live", cancellationToken);
						if (response.StatusCode >= HttpStatusCode.BadRequest)
							throw new($"Health check failed with status code: {response.StatusCode}.");
					}
				);
		}
		catch (Exception) {
			_eventStore.Dispose();
			throw;
		}
	}

	public void Stop() => _eventStore.Stop();

	public ValueTask DisposeAsync() {
		_httpClient?.Dispose();
		_eventStore?.Dispose();

        return new ValueTask(Task.CompletedTask);
	}

	static Version GetVersion() {
		const string versionPrefix = "EventStoreDB version";

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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

	void VerifyCertificatesExist() {
		var certificateFiles = new[] {
			Path.Combine("ca", "ca.crt"),
			Path.Combine("ca", "ca.key"),
			Path.Combine("node", "node.crt"),
			Path.Combine("node", "node.key")
		}.Select(path => Path.Combine(_hostCertificatePath, path));

		foreach (var file in certificateFiles)
			if (!File.Exists(file))
				throw new InvalidOperationException(
					$"Could not locate the certificates file {file} needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository."
				);
	}
}
