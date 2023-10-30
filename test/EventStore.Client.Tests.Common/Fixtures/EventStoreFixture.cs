using System.Net;
using EventStore.Client.Tests.FluentDocker;
using Serilog;

namespace EventStore.Client.Tests;

public delegate EventStoreFixtureOptions ConfigureFixture(EventStoreFixtureOptions options);

public record EventStoreFixtureOptions(EventStoreClientSettings ClientSettings, IDictionary<string, string> Environment) {
	// public EventStoreFixtureOptions UseCluster(bool useCluster = true) {
	// 	Environment["ES_USE_CLUSTER"] = useCluster.ToString();
	// 	return this;
	// }

	public EventStoreFixtureOptions RunInMemory(bool runInMemory = true) {
		Environment["EVENTSTORE_MEM_DB"] = runInMemory.ToString();
		return this;
	}

	public EventStoreFixtureOptions RunProjections(bool runProjections = true) {
		Environment["EVENTSTORE_START_STANDARD_PROJECTIONS"] = runProjections.ToString();
		Environment["EVENTSTORE_RUN_PROJECTIONS"]            = runProjections ? "All" : "None";
		return this;
	}

	public EventStoreFixtureOptions WithoutDefaultCredentials(bool withoutDefaultCredentials = true) {
		if (withoutDefaultCredentials)
			ClientSettings.DefaultCredentials = null;

		return this;
	}
}

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
			Options = new(new(), new Dictionary<string, string>());
			Service = new TestBypassService();
		}

		if (GlobalEnvironment.UseCluster) {
			Options = configure?.Invoke(EventStoreTestCluster.DefaultOptions()) ?? EventStoreTestCluster.DefaultOptions();
			Service = new EventStoreTestCluster(Options);
		}
		else {
			Options = configure?.Invoke(EventStoreTestNode.DefaultOptions()) ?? EventStoreTestNode.DefaultOptions();
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
	}
	
	public async Task InitializeAsync() {
		await Service.Start().ShouldNotThrowAsync();

		Logger.Information("*** !!! Warming up database !!! ***");

		Users = new(ClientSettings);
		await Users.WarmUp().ShouldNotThrowAsync();

		Streams = new(ClientSettings);
		await Streams.WarmUp().ShouldNotThrowAsync();

		if (Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None") {
			Projections = new(ClientSettings);
			await Projections.WarmUp().ShouldNotThrowAsync();
		}

		PersistentSubscriptions = new(ClientSettings);
		await PersistentSubscriptions.WarmUp().ShouldNotThrowAsync();

		Operations = new(ClientSettings);
		await Operations.WarmUp().ShouldNotThrowAsync();

		Logger.Information("Setup completed");

		await OnSetup().ShouldNotThrowAsync("Failed to run OnSetup!");
	}
	public async Task DisposeAsync() {
		try {
			await OnTearDown();
		}
		catch {
			// ignored
		}

		await Service.DisposeAsync();

		foreach (var testRunId in TestRuns)
			Logging.ReleaseLogs(testRunId);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

	
	public T NewClient<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() =>
		(T)Activator.CreateInstance(typeof(T), new object?[] { ClientSettings.With(configure) })!;
}

/// <summary>
/// The clients dont have default credentials set.
/// </summary>
public class InsecureClientTestFixture() : EventStoreFixture(x => x.WithoutDefaultCredentials());

public class RunInMemoryTestFixture() : EventStoreFixture(x => x.RunInMemory());

public class RunProjectionsTestFixture() : EventStoreFixture(x => x.RunProjections());


// public partial class EventStoreFixture : IAsyncLifetime, IAsyncDisposable {
// 	ILogger Logger { get; } = Log.ForContext<EventStoreFixture>();
// 	
// 	static EventStoreFixture() => ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
//
// 	public EventStoreFixture(ConfigureFixture? configure = null) {
// 		// TODO SS: should I verify the certificates exist here?
// 		if (GlobalEnvironment.UseExternalServer) {
// 			Options = new(new(), new Dictionary<string, string>());
// 			Service = new TestBypassService();
// 		}
//
// 		if (GlobalEnvironment.UseCluster) {
// 			Options = configure?.Invoke(EventStoreTestCluster.DefaultOptions()) ?? EventStoreTestCluster.DefaultOptions();
// 			Service = new EventStoreTestCluster(Options);
// 			
// 			// // fixture override
// 			// var useSingleNode = Options.Environment.TryGetValue("ES_USE_CLUSTER", out var value) && value == "false";
// 			//
// 			// if (useSingleNode) {
// 			// 	// remove the cluster environment variables
// 			// 	["ES_CERTS_CLUSTER"]                        = Path.Combine(Environment.CurrentDirectory, "certs-cluster"),
// 			// 	["EVENTSTORE_CLUSTER_SIZE"]                 = "3",
// 			// 	["EVENTSTORE_INT_TCP_PORT"]                 = "1112",
// 			// 	["EVENTSTORE_HTTP_PORT"]                    = "2113",
// 			// 	["EVENTSTORE_DISCOVER_VIA_DNS"]             = "false",
// 			// 	["EVENTSTORE_ENABLE_EXTERNAL_TCP"]          = "false",
// 			// 	["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
// 			// 	["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000",
// 			// 	["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]    = "true" // why true?
// 			// 	
// 			// 	Options.Environment.Remove("ES_USE_CLUSTER");
// 			// 	Options.Environment.Remove("EVENTSTORE_CLUSTER_SIZE");
// 			// 	Options.Environment.Remove("EVENTSTORE_INT_TCP_PORT");
// 			// 	Options.Environment.Remove("EVENTSTORE_HTTP_PORT");
// 			// 	Options.Environment.Remove("EVENTSTORE_DISCOVER_VIA_DNS");
// 			// 	Options.Environment.Remove("EVENTSTORE_ENABLE_EXTERNAL_TCP");
// 			// 	Options.Environment.Remove("EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE");
// 			// 	Options.Environment.Remove("EVENTSTORE_STREAM_INFO_CACHE_CAPACITY");
// 			// 	Options.Environment.Remove("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP");
// 			// 	
// 			// 	Options = EventStoreTestNode.DefaultOptions();
// 			// 	Service = new EventStoreTestNode(Options);
// 			// }
// 			// else {
// 			// 	Service = new EventStoreTestCluster(Options);
// 			// }
// 		}
// 		else {
// 			Options = configure?.Invoke(EventStoreTestNode.DefaultOptions()) ?? EventStoreTestNode.DefaultOptions();
// 			Service = new EventStoreTestNode(Options);
// 		}
// 	}
// 	
// 	// public EventStoreFixture(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) {
// 	// 	TestRunId = Logging.CaptureLogs(outputHelper);
// 	// 	Logger    = Log.ForContext<EventStoreFixture>();
// 	//
// 	// 	Logger.Information(">>> Test Run {testRunId} {Operation} <<<", TestRunId, "starting");
// 	//
// 	// 	// TODO SS: should I verify the certificates exist here?
// 	// 	if (GlobalEnvironment.UseExternalServer) {
// 	// 		Options = new(new(), new Dictionary<string, string>());
// 	// 		Service = new TestBypassService();
// 	// 	}
// 	//
// 	// 	if (GlobalEnvironment.UseCluster) {
// 	// 		Options = configure?.Invoke(EventStoreTestCluster.DefaultOptions()) ?? EventStoreTestCluster.DefaultOptions();
// 	// 		Service = new EventStoreTestCluster(Options);
// 	// 		
// 	// 		// // fixture override
// 	// 		// var useSingleNode = Options.Environment.TryGetValue("ES_USE_CLUSTER", out var value) && value == "false";
// 	// 		//
// 	// 		// if (useSingleNode) {
// 	// 		// 	// remove the cluster environment variables
// 	// 		// 	["ES_CERTS_CLUSTER"]                        = Path.Combine(Environment.CurrentDirectory, "certs-cluster"),
// 	// 		// 	["EVENTSTORE_CLUSTER_SIZE"]                 = "3",
// 	// 		// 	["EVENTSTORE_INT_TCP_PORT"]                 = "1112",
// 	// 		// 	["EVENTSTORE_HTTP_PORT"]                    = "2113",
// 	// 		// 	["EVENTSTORE_DISCOVER_VIA_DNS"]             = "false",
// 	// 		// 	["EVENTSTORE_ENABLE_EXTERNAL_TCP"]          = "false",
// 	// 		// 	["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
// 	// 		// 	["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]   = "10000",
// 	// 		// 	["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]    = "true" // why true?
// 	// 		// 	
// 	// 		// 	Options.Environment.Remove("ES_USE_CLUSTER");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_CLUSTER_SIZE");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_INT_TCP_PORT");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_HTTP_PORT");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_DISCOVER_VIA_DNS");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_ENABLE_EXTERNAL_TCP");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_STREAM_INFO_CACHE_CAPACITY");
// 	// 		// 	Options.Environment.Remove("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP");
// 	// 		// 	
// 	// 		// 	Options = EventStoreTestNode.DefaultOptions();
// 	// 		// 	Service = new EventStoreTestNode(Options);
// 	// 		// }
// 	// 		// else {
// 	// 		// 	Service = new EventStoreTestCluster(Options);
// 	// 		// }
// 	// 	}
// 	// 	else {
// 	// 		Options = configure?.Invoke(EventStoreTestNode.DefaultOptions()) ?? EventStoreTestNode.DefaultOptions();
// 	// 		Service = new EventStoreTestNode(Options);
// 	// 	}
// 	// }
// 	//
// 	// ILogger Logger { get; }
// 	//
// 	public Guid                     TestRunId { get; }
// 	public ITestService             Service   { get; }
// 	public EventStoreFixtureOptions Options   { get; }
//
// 	public EventStoreClient                        Streams                 { get; private set; } = null!;
// 	public EventStoreUserManagementClient          Users                   { get; private set; } = null!;
// 	public EventStoreProjectionManagementClient    Projections             { get; private set; } = null!;
// 	public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; private set; } = null!;
// 	public EventStoreOperationsClient              Operations              { get; private set; } = null!;
//
// 	// nice usability sugar
// 	public EventStoreFixture Fixture => this;
//
// 	public Func<Task> OnSetup    { get; set; } = () => Task.CompletedTask;
// 	public Func<Task> OnTearDown { get; set; } = () => Task.CompletedTask;
//
// 	/// <summary>
// 	/// must test this
// 	/// </summary>
// 	public EventStoreClientSettings ClientSettings =>
// 		new() {
// 			Interceptors             = Options.ClientSettings.Interceptors,
// 			ConnectionName           = Options.ClientSettings.ConnectionName,
// 			CreateHttpMessageHandler = Options.ClientSettings.CreateHttpMessageHandler,
// 			LoggerFactory            = Options.ClientSettings.LoggerFactory,
// 			ChannelCredentials       = Options.ClientSettings.ChannelCredentials,
// 			OperationOptions         = Options.ClientSettings.OperationOptions,
// 			ConnectivitySettings     = Options.ClientSettings.ConnectivitySettings,
// 			DefaultCredentials       = Options.ClientSettings.DefaultCredentials,
// 			DefaultDeadline          = Options.ClientSettings.DefaultDeadline
// 		};
//
// 	public T NewClient<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() =>
// 		(T)Activator.CreateInstance(typeof(T), new object?[] { ClientSettings.With(configure) })!;
// 	
// 	List<Guid> TestRuns = new();
// 	
// 	public void CaptureTestRun(ITestOutputHelper outputHelper) {
// 		TestRuns.Add(Logging.CaptureLogs(outputHelper));
// 		Logger.Information(">>> Test Run {testRunId} {Operation} <<<", TestRunId, "starting");
// 	}
//
// 	// public static EventStoreFixture Create(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) => new(outputHelper, configure);
//
// 	// public EventStoreFixture WithOnSetUp(Func<Task> onSetUp) {
// 	//     OnSetUp = onSetUp;
// 	//     return this;
// 	// }
// 	//
// 	// public EventStoreFixture WithOnTearDown(Func<Task> onTearDown) {
// 	//     OnTearDown = onTearDown;
// 	//     return this;
// 	// }
// 	//
// 	// public EventStoreFixture WithClientSettings(Func<EventStoreClientSettings, EventStoreClientSettings> configure) {
// 	//     Options = Options with { ClientSettings = configure(Options.ClientSettings) };
// 	//     return this;
// 	// }
//
//
// 	// public static async Task<EventStoreFixture> Initialize(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) {
// 	// 	var fixture = Create(outputHelper, configure);
// 	// 	await fixture.Setup();
// 	// 	return fixture;
// 	// }
//
// 	
// 	public async Task Setup() {
// 		await Service.Start();
//
// 		Logger.Information("*** !!! Warming up database !!! ***");
//
// 		Users = new(ClientSettings);
// 		await Users.WarmUp();
//
// 		Streams = new(ClientSettings);
// 		await Streams.WarmUp();
//
// 		if (Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None") {
// 			Projections = new(ClientSettings);
// 			await Projections.WarmUp();
// 		}
//
// 		PersistentSubscriptions = new(ClientSettings);
// 		await PersistentSubscriptions.WarmUp();
//
// 		Operations = new(ClientSettings);
// 		await Operations.WarmUp();
// 		
// 		// if (!WarmupCompleted.EnsureCalledOnce()) {
// 		// 	
// 		// }
// 		// else {
// 		// 	Logger.Information("*** >>> Skipping database warmup <<< ***");
// 		// }
// 		
// 		Logger.Information("Setup completed");
// 	}
//
// 	public async Task TearDown() {
// 		await Service.DisposeAsync();
//
// 		Logging.ReleaseLogs(TestRunId);
// 	}
//
// 	public async Task InitializeAsync() {
// 		await Fixture.Setup();
//
// 		try {
// 			await OnSetup();
// 		}
// 		catch (Exception ex) {
// 			throw new("Failed to run OnSetUp!", ex);
// 		}
// 	}
//
// 	public async Task DisposeAsync() {
// 		try {
// 			await OnTearDown();
// 		}
// 		catch {
// 			// ignored
// 		}
//
// 		await Fixture.TearDown();
//
// 		foreach (var testRunId in TestRuns) {
// 			TestRuns.Remove(testRunId);
// 		}
// 	}
//
// 	async ValueTask IAsyncDisposable.DisposeAsync() => await TearDown();
// }


// public abstract class EventStoreSharedFixture : IAsyncLifetime, IAsyncDisposable {
// 	// static readonly InterlockedBoolean WarmupCompleted = new InterlockedBoolean();
// 	
// 	static EventStoreSharedFixture() => ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
//
// 	public EventStoreSharedFixture() {
// 		
// 	}
//
// 	ILogger Logger { get; } = null!;
//
// 	public ITestService             Service { get; }
// 	public EventStoreFixtureOptions Options { get; }
//
// 	public EventStoreClient                        Streams                 { get; private set; } = null!;
// 	public EventStoreUserManagementClient          Users                   { get; private set; } = null!;
// 	public EventStoreProjectionManagementClient    Projections             { get; private set; } = null!;
// 	public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; private set; } = null!;
// 	public EventStoreOperationsClient              Operations              { get; private set; } = null!;
// 	
// 	/// <summary>
// 	/// must test this
// 	/// </summary>
// 	public EventStoreClientSettings ClientSettings =>
// 		new() {
// 			Interceptors             = Options.ClientSettings.Interceptors,
// 			ConnectionName           = Options.ClientSettings.ConnectionName,
// 			CreateHttpMessageHandler = Options.ClientSettings.CreateHttpMessageHandler,
// 			LoggerFactory            = Options.ClientSettings.LoggerFactory,
// 			ChannelCredentials       = Options.ClientSettings.ChannelCredentials,
// 			OperationOptions         = Options.ClientSettings.OperationOptions,
// 			ConnectivitySettings     = Options.ClientSettings.ConnectivitySettings,
// 			DefaultCredentials       = Options.ClientSettings.DefaultCredentials,
// 			DefaultDeadline          = Options.ClientSettings.DefaultDeadline
// 		};
//
// 	public T Client<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() =>
// 		(T)Activator.CreateInstance(typeof(T), new object?[] { ClientSettings.With(configure) })!;
//
// 	List<Guid> TestRuns { get; } = new();
//
// 	public abstract void Setup(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) { 
// 		var testRunId = Logging.CaptureLogs(outputHelper);
//
// 		TestRuns.Add(testRunId);
//
// 		Logger.Information(">>> Test Run {testRunId} {Operation} <<<", testRunId, "starting");
//
// 		// TODO SS: should I verify the certificates exist here?
// 		if (GlobalEnvironment.UseExternalServer) {
// 			Options = new(new(), new Dictionary<string, string>());
// 			Service = new TestBypassService();
// 		}
//
// 		if (GlobalEnvironment.UseCluster) {
// 			Options = configure?.Invoke(EventStoreTestCluster.DefaultOptions()) ?? EventStoreTestCluster.DefaultOptions();
// 			Service = new EventStoreTestCluster(Options);
// 		}
// 		else {
// 			Options = configure?.Invoke(EventStoreTestNode.DefaultOptions()) ?? EventStoreTestNode.DefaultOptions();
// 			Service = new EventStoreTestNode(Options);
// 		}
// 	}
// 	
// 	public async Task InitializeAsync() {
// 		await Service.Start();
//
// 		Logger.Information("*** !!! Warming up database !!! ***");
//
// 		Users = new(ClientSettings);
// 		await Users.WarmUp();
//
// 		Streams = new(ClientSettings);
// 		await Streams.WarmUp();
//
// 		if (Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None") {
// 			Projections = new(ClientSettings);
// 			await Projections.WarmUp();
// 		}
//
// 		PersistentSubscriptions = new(ClientSettings);
// 		await PersistentSubscriptions.WarmUp();
//
// 		Operations = new(ClientSettings);
// 		await Operations.WarmUp();
//
// 		Logger.Information("Setup completed");
//
// 		try {
// 			await OnSetup();
// 		}
// 		catch (Exception ex) {
// 			throw new("Failed to run OnSetUp!", ex);
// 		}
// 	}
//
// 	public async Task DisposeAsync() {
// 		try {
// 			await OnTearDown();
// 		}
// 		catch {
// 			// ignored
// 		}
//
// 		await Service.DisposeAsync();
//
// 		// foreach (var TestRunId in TestRuns) {
// 		// 	Logging.ReleaseLogs(TestRunId);
// 		// }
// 	}
//
// 	async ValueTask IAsyncDisposable.DisposeAsync() => await TearDown();
// }