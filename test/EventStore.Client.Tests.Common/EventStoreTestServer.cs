using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Polly;

namespace EventStore.Client {
	public class EventStoreTestServer : IEventStoreTestServer {
		private readonly string _hostCertificatePath;
		private readonly IContainerService _eventStore;
		private readonly HttpClient _httpClient;
		private static readonly string ContainerName = "es-client-dotnet-test";

		private static Version? _version;
		public static Version Version => _version ??= GetVersion();

		private static Version GetVersion() {
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
				    Version.TryParse(line[(versionPrefix.Length + 1)..].Split(' ')[0], out var version)) {
					return version;
				}
			}

			throw new InvalidOperationException("Could not determine server version.");
		}

		public EventStoreTestServer(
			string hostCertificatePath,
			Uri address,
			IDictionary<string, string>? envOverrides) {

			_hostCertificatePath = hostCertificatePath;
			VerifyCertificatesExist();

			_httpClient = new HttpClient(new SocketsHttpHandler {
				SslOptions = {RemoteCertificateValidationCallback = delegate { return true; }}
			}) {
				BaseAddress = address,
			};

			var env = new Dictionary<string, string> {
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
			foreach (var (key, value) in envOverrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
				env[key] = value;
			}

			_eventStore = new Builder()
				.UseContainer()
				.UseImage(GlobalEnvironment.DockerImage)
				.WithEnvironment(env.Select(pair => $"{pair.Key}={pair.Value}").ToArray())
				.WithName(ContainerName)
				.MountVolume(_hostCertificatePath, "/etc/eventstore/certs", MountType.ReadOnly)
				.ExposePort(2113, 2113)
				.WaitForHealthy(TimeSpan.FromSeconds(30))
				.ReuseIfExists()
				//.KeepContainer()
				//.KeepRunning()
				.Build();
		}


		private void VerifyCertificatesExist() {
			var certificateFiles = new[] {
				Path.Combine("ca", "ca.crt"),
				Path.Combine("ca", "ca.key"),
				Path.Combine("node", "node.crt"),
				Path.Combine("node", "node.key")
			}.Select(path => Path.Combine(_hostCertificatePath, path));

			foreach (var file in certificateFiles) {
				if (!File.Exists(file)) {
					throw new InvalidOperationException(
						$"Could not locate the certificates file {file} needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository.");
				}
			}
		}

		public async Task StartAsync(CancellationToken cancellationToken = default) {
			_eventStore.Start();
			try {
				await Policy.Handle<Exception>()
					.WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100))
					.ExecuteAsync(async () => {
						using var response = await _httpClient.GetAsync("/health/live", cancellationToken);
						if (response.StatusCode >= HttpStatusCode.BadRequest) {
							throw new Exception($"Health check failed with status code: {response.StatusCode}.");
						}
					});
			} catch (Exception) {
				_eventStore.Dispose();
				throw;
			}
		}

		public void Stop() {
			_eventStore.Stop();
		}

		public ValueTask DisposeAsync() {
			_httpClient?.Dispose();
			_eventStore?.Dispose();

			return new ValueTask(Task.CompletedTask);
		}
	}
}
