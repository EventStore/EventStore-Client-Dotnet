using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit.Sdk;

namespace EventStore.Client.Tests;

static class Logging {
	static readonly Subject<LogEvent>                       LogEventSubject = new();
	static readonly ConcurrentDictionary<Guid, IDisposable> Subscriptions   = new();

	static readonly MessageTemplateTextFormatter DefaultFormatter;

	static Logging() {
		DefaultFormatter = new("[{Timestamp:HH:mm:ss.fff} {Level:u3}] ({ThreadId:000}) {SourceContext} {Message}{NewLine}{Exception}");

		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(Application.Configuration)
			.WriteTo.Observers(x => x.Subscribe(LogEventSubject.OnNext))
			.CreateLogger();

#if GRPC_CORE
		    GrpcEnvironment.SetLogger(new GrpcCoreSerilogLogger(Log.Logger.ForContext<GrpcCoreSerilogLogger>()));
#endif

		Ductus.FluentDocker.Services.Logging.Enabled();

		AppDomain.CurrentDomain.DomainUnload += (_, _) => Log.CloseAndFlush();
	}

	public static void Initialize() { } // triggers static ctor

	/// <summary>
	///   Captures logs for the duration of the test run.
	/// </summary>
	static Guid CaptureLogs(Action<string> write, Guid testRunId = default) {
		if (testRunId == default)
			testRunId = Guid.NewGuid();

		var callContextData   = new AsyncLocal<Guid> { Value = testRunId };
		var testRunIdProperty = new LogEventProperty("TestRunId", new ScalarValue(testRunId));

		var subscription = LogEventSubject
			.Where(_ => callContextData.Value.Equals(testRunId))
			.Subscribe(WriteLogEvent());

		Subscriptions.TryAdd(testRunId, subscription);

		return testRunId;

		Action<LogEvent> WriteLogEvent() =>
			logEvent => {
				logEvent.AddPropertyIfAbsent(testRunIdProperty);
				using var writer = new StringWriter();
				DefaultFormatter.Format(logEvent, writer);
				write(writer.ToString().Trim());
			};
	}

	public static Guid CaptureLogs(ITestOutputHelper outputHelper, Guid testRunId = default) =>
		CaptureLogs(outputHelper.WriteLine, testRunId);

	public static Guid CaptureLogs(IMessageSink sink, Guid testRunId = default) =>
		CaptureLogs(msg => sink.OnMessage(new DiagnosticMessage(msg)), testRunId);

	public static void ReleaseLogs(Guid captureId) {
		if (!Subscriptions.TryRemove(captureId, out var subscription))
			return;

		try {
			subscription.Dispose();
		}
		catch {
			// ignored
		}
	}
}