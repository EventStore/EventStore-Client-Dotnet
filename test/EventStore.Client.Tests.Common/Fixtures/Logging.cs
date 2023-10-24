using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace EventStore.Client.Tests;

static class Logging {
    static readonly Subject<LogEvent> LogEventSubject = new();
    
    static readonly ConcurrentDictionary<Guid, IDisposable> Subscriptions = new();

    static Logging() {
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Information)
            .WriteTo.Observers(x => x.Subscribe(LogEventSubject.OnNext))
            .WriteTo.Seq("http://localhost:5341/", period: TimeSpan.FromMilliseconds(1));

        Log.Logger = loggerConfiguration.CreateLogger();

        Ductus.FluentDocker.Services.Logging.Enabled();

        #if GRPC_CORE
		    GrpcEnvironment.SetLogger(new GrpcCoreSerilogLogger(Log.Logger.ForContext<GrpcCoreSerilogLogger>()));
        #endif
        
        AppDomain.CurrentDomain.DomainUnload += (_, _) => Log.CloseAndFlush();
    }

    public static Guid CaptureLogs(ITestOutputHelper outputHelper, Guid captureId = default) {
        const string captureCorrelationId = nameof(captureCorrelationId);

        if (captureId == default)
            captureId = Guid.NewGuid();

        var callContextData = new AsyncLocal<(string CaptureCorrelationId, Guid CaptureId)> {
            Value = (captureCorrelationId, captureId)
        };

        var formatter = new MessageTemplateTextFormatter("{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message}");

        var formatterWithException = new MessageTemplateTextFormatter(
            "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message}{NewLine}{Exception}"
        );

        var subscription = LogEventSubject.Where(Filter).Subscribe(
            logEvent => {
                logEvent.AddOrUpdateProperty(new("CaptureId", new ScalarValue(captureId)));

                using var writer = new StringWriter();

                if (logEvent.Exception != null)
                    formatterWithException.Format(logEvent, writer);
                else
                    formatter.Format(logEvent, writer);

                outputHelper.WriteLine(writer.ToString());
            }
        );

        if (!Subscriptions.TryAdd(captureId, subscription)) {
            throw new Exception("WTF ConcurrentDictionary?!?");
        }

        return captureId;

        bool Filter(LogEvent logEvent) => callContextData.Value.CaptureId.Equals(captureId);
    }
    
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