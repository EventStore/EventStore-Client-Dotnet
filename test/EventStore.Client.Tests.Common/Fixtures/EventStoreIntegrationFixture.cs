using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using EventStore.Client;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit.Abstractions;

namespace EventStore.Tests.Fixtures;

public class EventStoreIntegrationFixture : IAsyncLifetime {
	static EventStoreIntegrationFixture() {
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		
		ConfigureLogging();
		
		return;
		
		static void ConfigureLogging() {
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

	static readonly Subject<LogEvent> LogEventSubject = new();
	
	protected EventStoreIntegrationFixture() {
		if (GlobalEnvironment.UseExternalServer) {
			Options = new EsTestDbOptions(new EventStoreClientSettings(), new Dictionary<string, string>(), new DirectoryInfo(""));
			Service = new EsTestNoOpServer();
		} 
		else if (GlobalEnvironment.UseCluster) {
			VerifyCertificatesExist(GlobalEnvironment.HostCertificateDirectory.FullName);
			// ReSharper disable once VirtualMemberCallInConstructor
			Service = new EsTestCluster(Options = Override(EsTestCluster.DefaultOptions() with {
				CertificateDirectory = GlobalEnvironment.HostCertificateDirectory
			}));
		} else {
			VerifyCertificatesExist(GlobalEnvironment.HostCertificateDirectory.FullName);
			// ReSharper disable once VirtualMemberCallInConstructor
			Service = new EsTestServer(Options = Override(EsTestServer.DefaultOptions() with {
				CertificateDirectory = GlobalEnvironment.HostCertificateDirectory
			}));
		}
	}
	
	ITestContainer Service { get; }
	IList<IDisposable> Disposables { get; } = new List<IDisposable>();
	
	protected EsTestDbOptions Options { get; }
	
	protected virtual EsTestDbOptions Override(EsTestDbOptions options) => options;
	
	static async Task<DirectoryInfo> EnsureCertificatesExist() {
		//var hostCertificatePath = Path.Combine(ProjectDir.Current, "..", "..", GlobalEnvironment.UseCluster ? "certs-cluster" : "certs");
		
		var directory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "certs"));

		if (!directory.Exists) {
			directory.Create();
		}

		var caCertificatePath = Path.Combine(directory.FullName, "ca");
		if (!Directory.Exists(caCertificatePath)) {
			await GenerateCertificates(
				directory.FullName,
				"A CA certificate & key file have been generated in the '/tmp/ca/' directory",
				"create-ca", "-out", "/tmp/ca"
			);
		}

		var nodeCertificatePath = Path.Combine(directory.FullName, "node");
		if (!Directory.Exists(nodeCertificatePath)) {
			await GenerateCertificates(
				directory.FullName,
				"A node certificate & key file have been generated in the '/tmp/node' directory.",
				"create-node", "-ca-certificate", "/tmp/ca/ca.crt", "-ca-key", "/tmp/ca/ca.key", "-out", "/tmp/node", "-ip-addresses", "127.0.0.1", "-dns-names", "localhost"
			);
		}

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
			} catch {
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

		foreach (var file in certificateFiles) {
			if (!File.Exists(file)) {
				throw new InvalidOperationException(
					$"Could not locate the certificates file {file} needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository.");
			}
		}
	}

	public async Task InitializeAsync() {
		Service.Start();
		
		try {
			await OnInitialized();
		} catch (Exception ex) {
			throw new("Failed to run OnInitialized!", ex);
		}
	}

	public async Task DisposeAsync() {
		try {
			await OnTearDown();

			foreach (var disposable in Disposables) {
				disposable.Dispose();
			}
			
		} catch {
			// ignored
		}

		Service.Dispose();
	}

	protected virtual Task OnInitialized() => Task.CompletedTask;

	protected virtual Task OnTearDown() => Task.CompletedTask;

	public void CaptureLogs(ITestOutputHelper testOutputHelper) {
		const string captureCorrelationId = nameof(captureCorrelationId);

		var captureId = Guid.NewGuid();

		var callContextData = new AsyncLocal<(string, Guid)> {
			Value = (captureCorrelationId, captureId)
		};

		bool Filter(LogEvent logEvent) =>
			callContextData.Value.Item2.Equals(captureId);

		MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(
			"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}");

		MessageTemplateTextFormatter formatterWithException
			= new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}");

		var subscription = LogEventSubject.Where(Filter).Subscribe(logEvent => {
			using var writer = new StringWriter();
			if (logEvent.Exception != null) {
				formatterWithException.Format(logEvent, writer);
			} else {
				formatter.Format(logEvent, writer);
			}

			testOutputHelper.WriteLine(writer.ToString());
		});

		Disposables.Add(subscription);
	}
}
