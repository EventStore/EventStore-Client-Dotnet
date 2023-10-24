using System.Net;
using EventStore.Client.Tests.FluentDocker;
using Serilog;

namespace EventStore.Client.Tests;

public delegate EventStoreFixtureOptions ConfigureFixture(EventStoreFixtureOptions options);

public record EventStoreFixtureOptions(EventStoreClientSettings ClientSettings, IDictionary<string, string> Environment, DirectoryInfo CertificateDirectory) {
    public EventStoreFixtureOptions UseCluster(bool useCluster = true) {
        Environment["ES_USE_CLUSTER"] = useCluster.ToString();
        return this;
    }

    public EventStoreFixtureOptions RunInMemory(bool runInMemory = true) {
        Environment["EVENTSTORE_MEM_DB"] = runInMemory.ToString();
        return this;
    }

    public EventStoreFixtureOptions WithProjections(bool withProjections = true) {
        Environment["EVENTSTORE_START_STANDARD_PROJECTIONS"] = withProjections.ToString();
        Environment["EVENTSTORE_RUN_PROJECTIONS"] = withProjections ? "All" :"None";
        return this;
    }
    
    public EventStoreFixtureOptions WithoutDefaultCredentials(bool withoutCredentials = true) {
        if (withoutCredentials)
            ClientSettings.DefaultCredentials = null;

        return this;
    }

    public EventStoreFixtureOptions WithRootCredentials(bool withRootCredentials = true) {
        if (withRootCredentials)
            ClientSettings.DefaultCredentials = TestCredentials.Root;

        return this;
    }
}

public partial class EventStoreFixture : IAsyncLifetime, IAsyncDisposable {
    static readonly ILogger Logger = Log.ForContext<EventStoreFixture>();

    static EventStoreFixture() {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
    }
    
    public EventStoreFixture(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) {
        TestRunId = Logging.CaptureLogs(outputHelper);
        
        if (GlobalEnvironment.UseExternalServer) {
            Service = new TestBypassService();
            Options = new(new(), new Dictionary<string, string>(), new(""));
        }

        if (GlobalEnvironment.UseCluster) {
            var options = EventStoreTestCluster.DefaultOptions() with {
                CertificateDirectory = GlobalEnvironment.CertificateDirectory
            };

            options = configure?.Invoke(options) ?? options;

            Service = new EventStoreTestCluster(options);
            Options = options;
        }
        else {
            CertificatesCommander.VerifyCertificatesExist(GlobalEnvironment.CertificateDirectory);

            var options = EventStoreTestNode.DefaultOptions() with {
                CertificateDirectory = GlobalEnvironment.CertificateDirectory
            };

            options = configure?.Invoke(options) ?? options;

            Service = new EventStoreTestNode(options);
            Options = options;
        }
        
        Streams                 = new(ClientSettings);
        Users                   = new(ClientSettings);
        Projections             = new(ClientSettings);
        PersistentSubscriptions = new(ClientSettings);
        Operations              = new(ClientSettings);
    }

    public Guid                     TestRunId { get; }
    public ITestService             Service   { get; }
    public EventStoreFixtureOptions Options   { get; }

    public EventStoreClient                        Streams                 { get; }
    public EventStoreUserManagementClient          Users                   { get; }
    public EventStoreProjectionManagementClient    Projections             { get; }
    public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; }
    public EventStoreOperationsClient              Operations              { get; }
    
    // nice usability sugar
    public EventStoreFixture Fixture => this;
    
    public Func<Task> OnSetUp    { get; set; } = () => Task.CompletedTask;
    public Func<Task> OnTearDown { get; set; } = () => Task.CompletedTask;
    
    /// <summary>
    /// must test this
    /// </summary>
    public EventStoreClientSettings ClientSettings =>
        new EventStoreClientSettings {
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

    public T Client<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() => 
        (T)Activator.CreateInstance(typeof(T), new object?[] { ClientSettings.With(configure) })!;

    public static EventStoreFixture Create(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) =>
        new(outputHelper, configure);

    // public EventStoreFixture WithOnSetUp(Func<Task> onSetUp) {
    //     OnSetUp = onSetUp;
    //     return this;
    // }
    //
    // public EventStoreFixture WithOnTearDown(Func<Task> onTearDown) {
    //     OnTearDown = onTearDown;
    //     return this;
    // }
    //
    // public EventStoreFixture WithClientSettings(Func<EventStoreClientSettings, EventStoreClientSettings> configure) {
    //     Options = Options with { ClientSettings = configure(Options.ClientSettings) };
    //     return this;
    // }

    public async Task SetUp() {
        Logger.Information("Starting container service...");
        
        await Service.Start();
        
        // Logger.Information("Warming up database...");
        //
        // await Streams.WarmUp();
        // await Users.WarmUp();
        // await Projections.WarmUp();
        // await PersistentSubscriptions.WarmUp();
        // await Operations.WarmUp();

        Logger.Information("Fixture Initialized");
    }

    public async Task TearDown() {
        Logger.Information("Stopping container service...");
        
        await Service.DisposeAsync();
        
        Logger.Information("Fixture Teardown");

        Logging.ReleaseLogs(TestRunId);
    }

    public static async Task<EventStoreFixture> Initialize(ITestOutputHelper outputHelper, ConfigureFixture? configure = null) {
        var fixture = Create(outputHelper, configure);
        await fixture.SetUp();
        return fixture;
    }

    public async Task InitializeAsync() {
        await Fixture.SetUp();

        try {
            await OnSetUp();
        }
        catch (Exception ex) {
            throw new("Failed to run OnSetUp!", ex);
        }
    }

    public async Task DisposeAsync() {
        try {
            await OnTearDown();
        }
        catch {
            // ignored
        }

        await Fixture.TearDown();
    }

    async ValueTask IAsyncDisposable.DisposeAsync() => await TearDown();
}