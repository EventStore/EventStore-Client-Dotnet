using System.Net;
using EventStore.Client.Tests.FluentDocker;
using Serilog;

namespace EventStore.Client.Tests;

public record EventStoreFixtureOptions(EventStoreClientSettings ClientSettings, IDictionary<string, string?> Environment) {
	public EventStoreFixtureOptions RunInMemory(bool runInMemory = true) =>
		this with { Environment = Environment.With(x => x["EVENTSTORE_MEM_DB"] = runInMemory.ToString()) };

	public EventStoreFixtureOptions RunProjections(bool runProjections = true) =>
		this with {
			Environment = Environment.With(
				x => {
					x["EVENTSTORE_START_STANDARD_PROJECTIONS"] = runProjections.ToString();
					x["EVENTSTORE_RUN_PROJECTIONS"]            = runProjections ? "All" : "None";
				}
			)
		};

	public EventStoreFixtureOptions WithoutDefaultCredentials() => 
		this with { ClientSettings = ClientSettings.With(x => x.DefaultCredentials = null) };
}

public delegate EventStoreFixtureOptions ConfigureFixture(EventStoreFixtureOptions options);

public partial class EventStoreFixture : IAsyncLifetime, IAsyncDisposable {
	static readonly ILogger Logger;

	static EventStoreFixture() {
		Logging.Initialize();
		Logger = Log.ForContext<EventStoreFixture>();
		
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
	}

	public EventStoreFixture() : this(options => options) { }
	
	protected EventStoreFixture(ConfigureFixture configure) {
		// TODO SS: should I verify the certificates exist here?
		if (GlobalEnvironment.UseExternalServer) {
			Options = new(new(), new Dictionary<string, string?>());
			Service = new TestBypassService();
		}

		if (GlobalEnvironment.UseCluster) {
			Options = configure(EventStoreTestCluster.DefaultOptions());
			Service = new EventStoreTestCluster(Options);
		}
		else {
			Options = configure(EventStoreTestNode.DefaultOptions());
			Service = new EventStoreTestNode(Options);
		}
	}

	List<Guid> TestRuns { get; } = new();
	
	public ITestService             Service { get; }
	public EventStoreFixtureOptions Options { get; }

	public EventStoreClient                        Streams                 { get; private set; } = null!;
	public EventStoreUserManagementClient          Users                   { get; private set; } = null!;
	public EventStoreProjectionManagementClient    Projections             { get; private set; } = null!;
	public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; private set; } = null!;
	public EventStoreOperationsClient              Operations              { get; private set; } = null!;
	
	public Func<Task> OnSetup    { get; set; } = () => Task.CompletedTask;
	public Func<Task> OnTearDown { get; set; } = () => Task.CompletedTask;

	/// <summary>
	/// must test this
	/// </summary>
	public EventStoreClientSettings ClientSettings =>
		new() {
			Interceptors             = Options.ClientSettings.Interceptors,
			ConnectionName           = Options.ClientSettings.ConnectionName,
			CreateHttpMessageHandler = Options.ClientSettings.CreateHttpMessageHandler,
			LoggerFactory            = Options.ClientSettings.LoggerFactory,
			ChannelCredentials       = Options.ClientSettings.ChannelCredentials,
			OperationOptions         = Options.ClientSettings.OperationOptions,
			ConnectivitySettings     = Options.ClientSettings.ConnectivitySettings,
			DefaultCredentials       = Options.ClientSettings.DefaultCredentials,
			DefaultDeadline          = Options.ClientSettings.DefaultDeadline
		};
	
	public void CaptureTestRun(ITestOutputHelper outputHelper) {
		var testRunId = Logging.CaptureLogs(outputHelper);
		TestRuns.Add(testRunId);
		Logger.Information(">>> Test Run {testRunId} {Operation} <<<", testRunId, "starting");
		Service.ReportStatus();
	}
	
	async Task WarmUp() {
		Logger.Information("*** !!! Warming up database !!! ***");

		Users = new(ClientSettings);
		await Users.WarmUp();

		Streams = new(ClientSettings);
		await Streams.WarmUp();

		if (Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None") {
			Projections = new(ClientSettings);
			await Projections.WarmUp();
		}

		PersistentSubscriptions = new(ClientSettings);
		await PersistentSubscriptions.WarmUp();

		Operations = new(ClientSettings);
		await Operations.WarmUp();
	}

	public async Task InitializeAsync() {
		await Service.Start();

		await WarmUp();

		await OnSetup();
	}
	
	public async Task DisposeAsync() {
		try {
			await OnTearDown();
		}
		catch {
			// ignored
		}

		await Service.DisposeAsync().AsTask().WithTimeout(TimeSpan.FromMinutes(5));

		foreach (var testRunId in TestRuns)
			Logging.ReleaseLogs(testRunId);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

}

/// <summary>
/// The clients dont have default credentials set.
/// </summary>
public class InsecureClientTestFixture() : EventStoreFixture(x => x.WithoutDefaultCredentials());

public class RunInMemoryTestFixture() : EventStoreFixture(x => x.RunInMemory());

public class RunProjectionsTestFixture() : EventStoreFixture(x => x.RunProjections());