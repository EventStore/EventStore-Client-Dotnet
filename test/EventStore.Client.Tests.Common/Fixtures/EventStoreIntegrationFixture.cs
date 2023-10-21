using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ductus.FluentDocker.Builders;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace EventStore.Client.Tests;

public abstract class EventStoreIntegrationFixture : IAsyncLifetime {
    static readonly Subject<LogEvent> LogEventSubject = new();

    static EventStoreIntegrationFixture() {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

        ConfigureLogging();

        return;

        static void ConfigureLogging() {

            Ductus.FluentDocker.Services.Logging.Enabled();
            
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

            AppDomain.CurrentDomain.DomainUnload += (_, __) => Log.CloseAndFlush();
        }
    }

    protected EventStoreIntegrationFixture() {
        //VerifyCertificatesExist(GlobalEnvironment.CertificateDirectory.FullName);
        // ReSharper disable once VirtualMemberCallInConstructor
        Service = new EventStoreTestCluster(
            Options = Override(
                EventStoreTestCluster.DefaultOptions() with {
                    CertificateDirectory = GlobalEnvironment.CertificateDirectory
                }
            )
        );
        
        //
        // if (GlobalEnvironment.UseExternalServer) {
        //     Options = new(new(), new Dictionary<string, string>(), new(""));
        //     Service = new EventStoreTestVoid();
        // }
        // else if (GlobalEnvironment.UseCluster) {
        //     VerifyCertificatesExist(GlobalEnvironment.CertificateDirectory.FullName);
        //     // ReSharper disable once VirtualMemberCallInConstructor
        //     Service = new EventStoreTestCluster(
        //         Options = Override(
        //             EventStoreTestCluster.DefaultOptions() with {
        //                 CertificateDirectory = GlobalEnvironment.CertificateDirectory
        //             }
        //         )
        //     );
        // }
        // else {
        //     VerifyCertificatesExist(GlobalEnvironment.CertificateDirectory.FullName);
        //     // ReSharper disable once VirtualMemberCallInConstructor
        //     Service = new EventStoreTestNode(
        //         Options = Override(
        //             EventStoreTestNode.DefaultOptions() with {
        //                 CertificateDirectory = GlobalEnvironment.CertificateDirectory
        //             }
        //         )
        //     );
        // }
    }

    ITestService       Service     { get; }
    IList<IDisposable> Disposables { get; } = new List<IDisposable>();

    public ITestService TestServer => Service;

    protected EventStoreTestServiceOptions Options { get; }

    public EventStoreTestServiceOptions GetOptions() => Options with { };
    
    public async Task InitializeAsync() {
        await Service.Start();

        try {
            await OnInitialized();
        }
        catch (Exception ex) {
            throw new("Failed to run OnInitialized!", ex);
        }
    }

    public async Task DisposeAsync() {
        try {
            await OnTearDown();

            foreach (var disposable in Disposables)
                disposable.Dispose();
        }
        catch {
            // ignored
        }

        await Service.DisposeAsync();
    }

    protected virtual EventStoreTestServiceOptions Override(EventStoreTestServiceOptions options) => options;

    static async Task<DirectoryInfo> EnsureCertificatesExist() {
        //var hostCertificatePath = Path.Combine(ProjectDir.Current, "..", "..", GlobalEnvironment.UseCluster ? "certs-cluster" : "certs");
        //var directory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "certs"));

        var directory = GlobalEnvironment.CertificateDirectory;

        if (!directory.Exists)
            directory.Create();

        var caCertificatePath = Path.Combine(directory.FullName, "ca");
        if (!Directory.Exists(caCertificatePath))
            await GenerateCertificates(
                directory.FullName,
                "A CA certificate & key file have been generated in the '/tmp/ca/' directory",
                "create-ca", "-out", "/tmp/ca"
            );

        var nodeCertificatePath = Path.Combine(directory.FullName, "node");
        if (!Directory.Exists(nodeCertificatePath))
            await GenerateCertificates(
                directory.FullName,
                "A node certificate & key file have been generated in the '/tmp/node' directory.",
                "create-node", "-ca-certificate", "/tmp/ca/ca.crt",
                "-ca-key", "/tmp/ca/ca.key", "-out",
                "/tmp/node", "-ip-addresses", "127.0.0.1",
                "-dns-names", "localhost"
            );

        static Task GenerateCertificates(string sourceFolder, string expectedLogMessage, string command, params string[] commandArgs) {
            using var container = new Builder()
                .UseContainer()
                .UseImage("eventstore/es-gencert-cli:1.0.1")
                .MountVolume(sourceFolder, "/tmp", Ductus.FluentDocker.Model.Builders.MountType.ReadWrite)
                .Command(command, commandArgs)
                .WaitForMessageInLog(expectedLogMessage, TimeSpan.FromSeconds(5))
                .Build();

            try {
                container.Start();
            }
            catch {
                container.Dispose();
            }

            return Task.CompletedTask;
        }

        VerifyCertificatesExist(directory.FullName);

        return directory;
    }

    static void VerifyCertificatesExist(string certificatePath) {
        var certificateFiles = new[] {
            Path.Combine("ca", "ca.crt"),
            Path.Combine("ca", "ca.key"),
            Path.Combine("node", "node.crt"),
            Path.Combine("node", "node.key")
        }.Select(path => Path.Combine(certificatePath, path));

        foreach (var file in certificateFiles)
            if (!File.Exists(file))
                throw new InvalidOperationException(
                    $"Could not locate the certificates file {file} needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository."
                );
    }

    protected virtual Task OnInitialized() => Task.CompletedTask;

    protected virtual Task OnTearDown() => Task.CompletedTask;

    public void CaptureLogs(ITestOutputHelper outputHelper) {
        const string captureCorrelationId = nameof(captureCorrelationId);

        var captureId = Guid.NewGuid();

        var callContextData = new AsyncLocal<(string, Guid)> {
            Value = (captureCorrelationId, captureId)
        };

        var formatter = new MessageTemplateTextFormatter(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}"
        );

        var formatterWithException = new MessageTemplateTextFormatter(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"
        );

        var subscription = LogEventSubject.Where(Filter).Subscribe(
            logEvent => {
                using var writer = new StringWriter();

                if (logEvent.Exception != null)
                    formatterWithException.Format(logEvent, writer);
                else
                    formatter.Format(logEvent, writer);

                outputHelper.WriteLine(writer.ToString());
            }
        );

        Disposables.Add(subscription);

        return;

        bool Filter(LogEvent logEvent) => callContextData.Value.Item2.Equals(captureId);
    }
}

// public abstract class EventStorePersistentIntegrationFixture {
//     static readonly Subject<LogEvent> LogEventSubject = new();
//
//     static EventStorePersistentIntegrationFixture() {
//         ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
//
//         ConfigureLogging();
//
//         return;
//
//         static void ConfigureLogging() {
//
//             Ductus.FluentDocker.Services.Logging.Enabled();
//             
//             var loggerConfiguration = new LoggerConfiguration()
//                 .Enrich.FromLogContext()
//                 .MinimumLevel.Is(LogEventLevel.Verbose)
//                 .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
//                 .MinimumLevel.Override("Grpc", LogEventLevel.Verbose)
//                 .WriteTo.Observers(observable => observable.Subscribe(LogEventSubject.OnNext))
//                 .WriteTo.Seq("http://localhost:5341/", period: TimeSpan.FromMilliseconds(1));
//
//             Log.Logger = loggerConfiguration.CreateLogger();
//
// #if GRPC_CORE
// 			GrpcEnvironment.SetLogger(new GrpcCoreSerilogLogger(Log.Logger.ForContext<GrpcCoreSerilogLogger>()));
// #endif
//
//             AppDomain.CurrentDomain.DomainUnload += (_, __) => Log.CloseAndFlush();
//         }
//     }
//
//     static ITestService       Service     { get; } = null!;
//     static InterlockedBoolean Initialized { get; } = new();
//     
//     public static async Task Initialize(Func<EventStoreTestServiceOptions, EventStoreTestServiceOptions> configure) {
//         var options = EventStoreTestCluster.DefaultOptions() with {
//             CertificateDirectory = GlobalEnvironment.CertificateDirectory
//         };
//         
//         options = configure(options);
//
//         Service = new EventStoreTestCluster(
//             Options = Override(
//                 EventStoreTestCluster.DefaultOptions() with {
//                     CertificateDirectory = GlobalEnvironment.CertificateDirectory
//                 }
//             )
//         );
//         
//         
//         Service.Start();
//     }
// }