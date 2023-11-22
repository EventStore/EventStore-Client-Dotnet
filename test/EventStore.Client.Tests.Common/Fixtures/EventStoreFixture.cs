using System.Net;
using EventStore.Client.Tests.FluentDocker;
using Serilog;
using static System.TimeSpan;

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

[PublicAPI]
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

	public EventStoreClient                        Streams       { get; private set; } = null!;
	public EventStoreUserManagementClient          Users         { get; private set; } = null!;
	public EventStoreProjectionManagementClient    Projections   { get; private set; } = null!;
	public EventStorePersistentSubscriptionsClient Subscriptions { get; private set; } = null!;
	public EventStoreOperationsClient              Operations    { get; private set; } = null!;

	public Func<Task> OnSetup    { get; init; } = () => Task.CompletedTask;
	public Func<Task> OnTearDown { get; init; } = () => Task.CompletedTask;

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
	
	InterlockedBoolean WarmUpCompleted  { get; } = new InterlockedBoolean();
	SemaphoreSlim      WarmUpGatekeeper { get; } = new(1, 1);
	
	public void CaptureTestRun(ITestOutputHelper outputHelper) {
		var testRunId = Logging.CaptureLogs(outputHelper);
		TestRuns.Add(testRunId);
		Logger.Information(">>> Test Run {TestRunId} {Operation} <<<", testRunId, "starting");
		Service.ReportStatus();
	}
	
	public async Task InitializeAsync() {
		await Service.Start();

		await WarmUpGatekeeper.WaitAsync();
		
		try {
			if (!WarmUpCompleted.CurrentValue) {
				Logger.Warning("*** Warmup started ***");

				await Task.WhenAll(
					InitClient<EventStoreUserManagementClient>(async x => Users = await x.WarmUp()),
					InitClient<EventStoreClient>(async x => Streams = await x.WarmUp()),
					InitClient<EventStoreProjectionManagementClient>(async x => Projections = await x.WarmUp(), Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None"),
					InitClient<EventStorePersistentSubscriptionsClient>(async x => Subscriptions = await x.WarmUp()),
					InitClient<EventStoreOperationsClient>(async x => Operations = await x.WarmUp())
				);
				
				WarmUpCompleted.EnsureCalledOnce();
				
				Logger.Warning("*** Warmup completed ***");
			}
			else {
				Logger.Information("*** Warmup skipped ***");
			}
		}
		finally {
			WarmUpGatekeeper.Release();
		}
		
		await OnSetup();
		
		return;

		async Task<T> InitClient<T>(Func<T, Task> action, bool execute = true) where T : EventStoreClientBase {
			if (!execute) return default(T)!;
			var client = (Activator.CreateInstance(typeof(T), new object?[] { ClientSettings }) as T)!;
			await action(client);
			return client;
		}
	}

	public async Task DisposeAsync() {
		try {
			await OnTearDown();
		}
		catch {
			// ignored
		}

		await Service.DisposeAsync().AsTask().WithTimeout(FromMinutes(5));

		foreach (var testRunId in TestRuns)
			Logging.ReleaseLogs(testRunId);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}