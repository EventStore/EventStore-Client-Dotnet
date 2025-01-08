// ReSharper disable InconsistentNaming

using System.Net;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Services.Extensions;
using EventStore.Client;
using Kurrent.Client.Tests.FluentDocker;
using Serilog;
using static System.TimeSpan;
using KurrentClient = EventStore.Client.KurrentClient;

namespace Kurrent.Client.Tests;

[PublicAPI]
public partial class KurrentPermanentFixture : IAsyncLifetime, IAsyncDisposable {
	static readonly ILogger Logger;

	static KurrentPermanentFixture() {
		Logging.Initialize();
		Logger = Serilog.Log.ForContext<KurrentPermanentFixture>();

#if NET9_0_OR_GREATER
		var httpClientHandler = new HttpClientHandler();
		httpClientHandler.ServerCertificateCustomValidationCallback = delegate { return true; };
#else
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#endif
	}

	public KurrentPermanentFixture() : this(options => options) { }

	protected KurrentPermanentFixture(ConfigureFixture configure) {
		Options = configure(KurrentPermanentTestNode.DefaultOptions());
		Service = new KurrentPermanentTestNode(Options);
	}

	List<Guid> TestRuns { get; } = new();

	public ILogger Log => Logger;

	public ITestService          Service { get; }
	public KurrentFixtureOptions Options { get; }
	public Faker                 Faker   { get; } = new Faker();

	public Version EventStoreVersion               { get; private set; } = null!;
	public bool    EventStoreHasLastStreamPosition { get; private set; }

	public KurrentClient                        Streams       { get; private set; } = null!;
	public KurrentUserManagementClient          Users         { get; private set; } = null!;
	public KurrentProjectionManagementClient    Projections   { get; private set; } = null!;
	public KurrentPersistentSubscriptionsClient Subscriptions { get; private set; } = null!;
	public KurrentOperationsClient              Operations    { get; private set; } = null!;

	public bool SkipPsWarmUp { get; set; }

	public Func<Task> OnSetup    { get; init; } = () => Task.CompletedTask;
	public Func<Task> OnTearDown { get; init; } = () => Task.CompletedTask;

	/// <summary>
	/// must test this
	/// </summary>
	public KurrentClientSettings ClientSettings =>
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

	InterlockedBoolean            WarmUpCompleted { get; } = new InterlockedBoolean();
	static readonly SemaphoreSlim WarmUpGatekeeper = new(1, 1);

	public void CaptureTestRun(ITestOutputHelper outputHelper) {
		var testRunId = Logging.CaptureLogs(outputHelper);
		TestRuns.Add(testRunId);
		Logger.Information(">>> Test Run {TestRunId} {Operation} <<<", testRunId, "starting");
		Service.ReportStatus();
	}

	public async Task InitializeAsync() {
		await WarmUpGatekeeper.WaitAsync();

		try {
			await Service.Start();
			EventStoreVersion               = GetKurrentVersion();
			EventStoreHasLastStreamPosition = (EventStoreVersion?.Major ?? int.MaxValue) >= 21;

			if (!WarmUpCompleted.CurrentValue) {
				Logger.Warning("*** Warmup started ***");

				await Task.WhenAll(
					InitClient<KurrentUserManagementClient>(async x => Users = await x.WarmUp()),
					InitClient<KurrentClient>(async x => Streams             = await x.WarmUp()),
					InitClient<KurrentProjectionManagementClient>(
						async x => Projections = await x.WarmUp(),
						Options.Environment["EVENTSTORE_RUN_PROJECTIONS"] != "None"
					),
					InitClient<KurrentPersistentSubscriptionsClient>(async x => Subscriptions = SkipPsWarmUp ? x : await x.WarmUp()),
					InitClient<KurrentOperationsClient>(async x => Operations                 = await x.WarmUp())
				);

				WarmUpCompleted.EnsureCalledOnce();

				Logger.Warning("*** Warmup completed ***");
			} else {
				Logger.Information("*** Warmup skipped ***");
			}
		} finally {
			WarmUpGatekeeper.Release();
		}

		await OnSetup();

		return;

		async Task<T> InitClient<T>(Func<T, Task> action, bool execute = true) where T : KurrentClientBase {
			if (!execute) return default(T)!;

			var client = (Activator.CreateInstance(typeof(T), ClientSettings) as T)!;
			await action(client);
			return client;
		}

		static Version GetKurrentVersion() {
			const string versionPrefix = "EventStoreDB version";

			using var cancellator = new CancellationTokenSource(FromSeconds(30));
			using var eventstore = new Builder()
				.UseContainer()
				.UseImage(GlobalEnvironment.DockerImage)
				.Command("--version")
				.Build()
				.Start();

			using var log = eventstore.Logs(true, cancellator.Token);
			foreach (var line in log.ReadToEnd()) {
				Logger.Information("EventStoreDB: {Line}", line);
				if (line.StartsWith(versionPrefix) &&
				    Version.TryParse(
					    new string(ReadVersion(line[(versionPrefix.Length + 1)..]).ToArray()),
					    out var version
				    )) {
					return version;
				}
			}

			throw new InvalidOperationException("Could not determine server version.");

			IEnumerable<char> ReadVersion(string s) {
				foreach (var c in s.TakeWhile(c => c == '.' || char.IsDigit(c))) {
					yield return c;
				}
			}
		}
	}

	public async Task DisposeAsync() {
		try {
			await OnTearDown();
		} catch {
			// ignored
		}

		await Service.DisposeAsync().AsTask().WithTimeout(FromMinutes(5));

		foreach (var testRunId in TestRuns)
			Logging.ReleaseLogs(testRunId);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}

public abstract class KurrentPermanentTests<TFixture> : IClassFixture<TFixture> where TFixture : KurrentPermanentFixture {
	protected KurrentPermanentTests(ITestOutputHelper output, TFixture fixture) => Fixture = fixture.With(x => x.CaptureTestRun(output));

	protected TFixture Fixture { get; }
}
