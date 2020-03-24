using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixtureBase : IAsyncLifetime {
		public const string TestEventType = "-";

		private static readonly Subject<LogEvent> LogEventSubject = new Subject<LogEvent>();
		private readonly IList<IDisposable> _disposables;
		protected EventStoreTestServer TestServer { get; }
		protected EventStoreClientSettings Settings { get; }

		static EventStoreClientFixtureBase() {
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

		protected EventStoreClientFixtureBase(EventStoreClientSettings? clientSettings,
			IDictionary<string, string>? env = null) {
			_disposables = new List<IDisposable>();

			Settings = clientSettings ?? new EventStoreClientSettings {
				OperationOptions = {
					TimeoutAfter = Debugger.IsAttached
						? new TimeSpan?()
						: TimeSpan.FromSeconds(30)
				},
				CreateHttpMessageHandler = () => new SocketsHttpHandler {
					SslOptions = new SslClientAuthenticationOptions {
						RemoteCertificateValidationCallback = delegate { return true; }
					}
				}
			};

			TestServer = new EventStoreTestServer(env);
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

			bool Filter(LogEvent logEvent) => callContextData.Value!.Item2.Equals(captureId);

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
			private readonly DockerContainer _container;
			private readonly HttpClient _httpClient;

			public EventStoreTestServer(IDictionary<string, string>? env) {
				_httpClient = new HttpClient(new SocketsHttpHandler {
					SslOptions = {
						RemoteCertificateValidationCallback = delegate { return true; }
					}
				}) {
					BaseAddress = new UriBuilder {
						Scheme = Uri.UriSchemeHttps,
						Port = 2113
					}.Uri
				};
				var tag = Environment.GetEnvironmentVariable("ES_DOCKER_TAG") ?? "6.0.0-preview3-bionic";
				_container = new DockerContainer("eventstore/eventstore", tag, async ct => {
					try {
						using var response = await _httpClient.GetAsync("/web/index.html", ct);
						return (int)response.StatusCode < 400;
					} catch {
					}

					return false;
				}, new Dictionary<int, int> {
					[2113] = 2113
				}) {
					Env = new Dictionary<string, string>(
						env ?? Enumerable.Empty<KeyValuePair<string, string>>()) {
						["EVENTSTORE_DEV"] = "true"
					},
					ContainerName = "es-client-dotnet-test"
				};
			}

			public Task Start(CancellationToken cancellationToken = default) => _container.Start(cancellationToken);

			public ValueTask DisposeAsync() {
				_httpClient.Dispose();
				return _container.DisposeAsync();
			}
		}
	}
}
