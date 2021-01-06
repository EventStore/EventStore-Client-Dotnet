using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
#if GRPC_CORE
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
#endif
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Display;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixtureBase : IAsyncLifetime {
		public const string TestEventType = "-";

		private const string ConnectionString =
#if GRPC_CORE
				"esdb://127.0.0.1:2113/?tlsVerifyCert=false"
#else
				"esdb://localhost:2113/?tlsVerifyCert=false"
#endif
			;

		private static readonly Subject<LogEvent> LogEventSubject = new Subject<LogEvent>();

		private static readonly string HostCertificatePath =
			Path.GetFullPath(Path.Combine("..", "..", "..", "..", "certs"), Environment.CurrentDirectory);

		private readonly IList<IDisposable> _disposables;
		protected EventStoreTestServer TestServer { get; }
		protected EventStoreClientSettings Settings { get; }

		static EventStoreClientFixtureBase() {
			ConfigureLogging();

			VerifyCertificates();
		}

		private static void ConfigureLogging() {
			var loggerConfiguration = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.MinimumLevel.Is(LogEventLevel.Verbose)
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.MinimumLevel.Override("Grpc", LogEventLevel.Warning)
				.WriteTo.Observers(observable => observable.Subscribe(LogEventSubject.OnNext))
				.WriteTo.Seq("http://localhost:5341/", period: TimeSpan.FromMilliseconds(1));
			Log.Logger = loggerConfiguration.CreateLogger();

			AppDomain.CurrentDomain.DomainUnload += (_, e) => Log.CloseAndFlush();
		}

		private static void VerifyCertificates() {
			var certificateFiles = new[] {
				Path.Combine("ca", "ca.crt"),
				Path.Combine("ca", "ca.key"),
				Path.Combine("node", "node.crt"),
				Path.Combine("node", "node.key")
			}.Select(path => Path.Combine(HostCertificatePath, path));
			if (certificateFiles.Any(file => !File.Exists(file))) {
				throw new InvalidOperationException(
					"Could not locate the certificates needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository.");
			}
		}

		protected EventStoreClientFixtureBase(EventStoreClientSettings? clientSettings,
			IDictionary<string, string>? env = null) {
			_disposables = new List<IDisposable>();
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			Settings = clientSettings ?? EventStoreClientSettings.Create(ConnectionString);

			Settings.OperationOptions.TimeoutAfter = Debugger.IsAttached
				? new TimeSpan?()
				: TimeSpan.FromSeconds(30);

#if GRPC_CORE
			Settings.ChannelCredentials ??= GetServerCertificate();

			static SslCredentials GetServerCertificate() => new SslCredentials(
				File.ReadAllText(Path.Combine(HostCertificatePath, "ca", "ca.crt")), null, _ => true);
#endif

			Settings.LoggerFactory ??= new SerilogLoggerFactory();

			TestServer = new EventStoreTestServer(Settings.ConnectivitySettings.Address, env);
		}

		protected abstract Task Given();
		protected abstract Task When();

		public IEnumerable<EventData> CreateTestEvents(int count = 1, string? type = null)
			=> Enumerable.Range(0, count).Select(index => CreateTestEvent(index, type ?? TestEventType));

		protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType);

		protected static EventData CreateTestEvent(int index, string type)
			=> new EventData(Uuid.NewUuid(), type, Encoding.UTF8.GetBytes($@"{{""x"":{index}}}"));

		public virtual async Task InitializeAsync() {
			await TestServer.Start().WithTimeout(TimeSpan.FromMinutes(5));
			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}

		public virtual Task DisposeAsync() {
			foreach (var disposable in _disposables) {
				disposable.Dispose();
			}

			return TestServer.DisposeAsync().AsTask();
		}

		public string GetStreamName([CallerMemberName] string? testMethod = null) {
			var type = GetType();

			return $"{type.DeclaringType?.Name}.{testMethod ?? "unknown"}";
		}

		public void CaptureLogs(ITestOutputHelper testOutputHelper) {
			const string captureCorrelationId = nameof(captureCorrelationId);

			var captureId = Guid.NewGuid();

			var callContextData = new AsyncLocal<Tuple<string, Guid>> {
				Value = new Tuple<string, Guid>(captureCorrelationId, captureId)
			};

			bool Filter(LogEvent logEvent) => callContextData!.Value!.Item2.Equals(captureId);

			MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(
				"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}");

			MessageTemplateTextFormatter formatterWithException =
				new MessageTemplateTextFormatter(
					"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}");

			var subscription = LogEventSubject.Where(Filter).Subscribe(logEvent => {
				using var writer = new StringWriter();
				if (logEvent.Exception != null) {
					formatterWithException.Format(logEvent, writer);
				} else {
					formatter.Format(logEvent, writer);
				}

				testOutputHelper.WriteLine(writer.ToString());
			});

			_disposables.Add(subscription);
		}

		protected class EventStoreTestServer : IAsyncDisposable {
			private readonly IContainerService _eventStore;
			private readonly HttpClient _httpClient;
			private static readonly string ContainerName = "es-client-dotnet-test";

			public EventStoreTestServer(Uri address, IDictionary<string, string>? envOverrides) {
				_httpClient = new HttpClient(new SocketsHttpHandler {
					SslOptions = {
						RemoteCertificateValidationCallback = delegate { return true; }
					}
				}) {
					BaseAddress = address,
				};
				var tag = Environment.GetEnvironmentVariable("ES_DOCKER_TAG") ?? "ci";

				var env = new Dictionary<string, string> {
					["EVENTSTORE_MEM_DB"] = "true",
					["EVENTSTORE_CERTIFICATE_FILE"] = "/etc/eventstore/certs/node/node.crt",
					["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"] = "/etc/eventstore/certs/node/node.key",
					["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"] = "/etc/eventstore/certs/ca",
					["EVENTSTORE_LOG_LEVEL"] = "Verbose",
					["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"] = "True"
				};
				foreach (var (key, value) in envOverrides ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
					env[key] = value;
				}

				_eventStore = new Builder()
					.UseContainer()
					.UseImage($"docker.pkg.github.com/eventstore/eventstore/eventstore:{tag}")
					.WithEnvironment(env.Select(pair => $"{pair.Key}={pair.Value}").ToArray())
					.WithName(ContainerName)
					.MountVolume(HostCertificatePath, "/etc/eventstore/certs", MountType.ReadOnly)
					.ExposePort(2113, 2113)
					.Build();
			}

			public async Task Start(CancellationToken cancellationToken = default) {
				_eventStore.Start();
				try {
					await Policy.Handle<Exception>()
						.WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(retryCount * retryCount))
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

			public ValueTask DisposeAsync() {
				_httpClient?.Dispose();
				_eventStore?.Dispose();

				return new ValueTask(Task.CompletedTask);
			}
		}
	}
}
