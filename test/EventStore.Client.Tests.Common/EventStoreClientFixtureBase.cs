using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Display;
using Xunit;
using Xunit.Abstractions;

namespace EventStore.Client {
	public abstract class EventStoreClientFixtureBase : IAsyncLifetime {
		public const string TestEventType = "-";

		private const string ConnectionStringSingle = "esdb://admin:changeit@localhost:2113/?tlsVerifyCert=false";
		private const string ConnectionStringCluster = "esdb://admin:changeit@localhost:2113,localhost:2112,localhost:2111?tls=true&tlsVerifyCert=false";

		private static readonly Subject<LogEvent> LogEventSubject = new Subject<LogEvent>();

		private readonly IList<IDisposable> _disposables;
		public IEventStoreTestServer TestServer { get; }
		protected EventStoreClientSettings Settings { get; }

		static EventStoreClientFixtureBase() {
			ConfigureLogging();
		}

		private static void ConfigureLogging() {
			var loggerConfiguration = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.MinimumLevel.Is(LogEventLevel.Verbose)
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.MinimumLevel.Override("Grpc", LogEventLevel.Verbose)
				.WriteTo.Observers(observable => observable.Subscribe(LogEventSubject.OnNext))
				.WriteTo.Seq("http://localhost:5341/", period: TimeSpan.FromMilliseconds(1));
			Log.Logger = loggerConfiguration.CreateLogger();
#if GRPC_CORE
			GrpcEnvironment.SetLogger(new GrpcCoreSerilogLogger(Log.Logger.ForContext<GrpcCoreSerilogLogger>()));
#endif
			AppDomain.CurrentDomain.DomainUnload += (_, e) => Log.CloseAndFlush();
		}

		protected EventStoreClientFixtureBase(EventStoreClientSettings? clientSettings,
			IDictionary<string, string>? env = null, bool noDefaultCredentials = false) {
			_disposables = new List<IDisposable>();
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			var connectionString = GlobalEnvironment.UseCluster ? ConnectionStringCluster : ConnectionStringSingle;
			Settings = clientSettings ?? EventStoreClientSettings.Create(connectionString);

			if (noDefaultCredentials) {
				Settings.DefaultCredentials = null;
			}
			
			Settings.DefaultDeadline = Debugger.IsAttached
				? new TimeSpan?()
				: TimeSpan.FromSeconds(30);

			var hostCertificatePath = Path.Combine(ProjectDir.Current, "..", "..",
				GlobalEnvironment.UseCluster ? "certs-cluster" : "certs");

			Settings.LoggerFactory ??= new SerilogLoggerFactory();

			Settings.ConnectivitySettings.MaxDiscoverAttempts = 20;
			Settings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1);

			if (GlobalEnvironment.UseExternalServer) {
				TestServer = new EventStoreTestServerExternal();
			} else {
				TestServer = GlobalEnvironment.UseCluster
					? new EventStoreTestServerCluster(hostCertificatePath, Settings.ConnectivitySettings.Address, env) 
					: new EventStoreTestServer(hostCertificatePath, Settings.ConnectivitySettings.Address, env);
			}
		}

		protected abstract Task OnServerUpAsync();
		protected abstract Task Given();
		protected abstract Task When();

		public IEnumerable<EventData> CreateTestEvents(int count = 1, string? type = null, int metadataSize = 1)
			=> Enumerable.Range(0, count).Select(index => CreateTestEvent(index, type ?? TestEventType, metadataSize));

		protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType, 1);

		protected static EventData CreateTestEvent(int index, string type, int metadataSize)
			=> new EventData(
				eventId: Uuid.NewUuid(),
				type: type,
				data: Encoding.UTF8.GetBytes($@"{{""x"":{index}}}"),
				metadata: Encoding.UTF8.GetBytes("\"" + new string('$', metadataSize) + "\""));

		public virtual async Task InitializeAsync() {
			await TestServer.StartAsync().WithTimeout(TimeSpan.FromMinutes(5));
			await OnServerUpAsync().WithTimeout(TimeSpan.FromMinutes(5));
			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}

		public virtual Task DisposeAsync() {
			foreach (var disposable in _disposables) {
				disposable.Dispose();
			}

			return TestServer.DisposeAsync().AsTask().WithTimeout(TimeSpan.FromMinutes(5));
		}

		public string GetStreamName([CallerMemberName] string? testMethod = null) {
			var type = GetType();

			return $"{type.DeclaringType?.Name}.{testMethod ?? "unknown"}";
		}

		public void CaptureLogs(ITestOutputHelper testOutputHelper) {
			const string captureCorrelationId = nameof(captureCorrelationId);

			var captureId = Guid.NewGuid();

			var callContextData = new AsyncLocal<(string, Guid)> {
				Value = (captureCorrelationId, captureId)
			};

			bool Filter(LogEvent logEvent) => callContextData.Value.Item2.Equals(captureId);

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
	}
}
