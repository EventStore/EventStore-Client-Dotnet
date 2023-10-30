using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.SystemConsole.Themes;
using Xunit.Sdk;

namespace EventStore.Client.Tests;

static class Logging {
	static readonly Subject<LogEvent>                       LogEventSubject = new();
	static readonly ConcurrentDictionary<Guid, IDisposable> Subscriptions   = new();

    static readonly MessageTemplateTextFormatter DefaultFormatter;
    
    static Logging() {
	    DefaultFormatter = new("[{Timestamp:HH:mm:ss.fff} {Level:u3}] {TestRunId} ({ThreadId:000}) {SourceContext} {Message}{NewLine}{Exception}");
	    
	    Log.Logger = new LoggerConfiguration()
	        .Enrich.WithProperty(Serilog.Core.Constants.SourceContextPropertyName, "EventStore.Client.Tests")
            .Enrich.FromLogContext()
	        .Enrich.WithThreadId()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
	        .MinimumLevel.Override("Grpc", LogEventLevel.Verbose)
	        //.MinimumLevel.Override("EventStore.Client.SharingProvider", LogEventLevel.Information)
            .WriteTo.Observers(x => x.Subscribe(LogEventSubject.OnNext))
	        .WriteTo.Logger(
		        logger => logger.WriteTo.Console(
			        theme: AnsiConsoleTheme.Literate, 
			        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {TestRunId} ({ThreadId:000}) {SourceContext} {Message}{NewLine}{Exception}", 
			        applyThemeToRedirectedOutput: true
			    )
	        )
            .WriteTo.Seq("http://localhost:5341/", period: TimeSpan.FromMilliseconds(1))
	        .CreateLogger();
	    
        #if GRPC_CORE
		    GrpcEnvironment.SetLogger(new GrpcCoreSerilogLogger(Log.Logger.ForContext<GrpcCoreSerilogLogger>()));
        #endif
	    
	    Ductus.FluentDocker.Services.Logging.Enabled();
        
        AppDomain.CurrentDomain.DomainUnload += (_, _) => Log.CloseAndFlush();
    }

    public static void Initialize() { } // triggers static ctor

	static Guid CaptureLogs(Action<string> write, Guid testRunId = default) {
	    if (testRunId == default)
		    testRunId = Guid.NewGuid();

	    var callContextData   = new AsyncLocal<Guid> { Value = testRunId };
	    var testRunIdProperty = new LogEventProperty("TestRunId", new ScalarValue(testRunId));

	    var subscription = LogEventSubject
		    .Where(_ => callContextData.Value.Equals(testRunId))
		    .Subscribe(
			    logEvent => {
				    logEvent.AddOrUpdateProperty(testRunIdProperty);
				    using var writer = new StringWriter();
				    DefaultFormatter.Format(logEvent, writer);
				    write(writer.ToString().Trim());
			    }
		    );
	    
	    Subscriptions.TryAdd(testRunId, subscription);

	    return testRunId;
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