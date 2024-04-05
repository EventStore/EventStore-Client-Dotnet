using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EventStore.Client;

/// <summary>
/// The base class used by clients used to communicate with the EventStoreDB.
/// </summary>
public abstract class EventStoreClientBase : IDisposable, IAsyncDisposable {
	// for grpc.net we can dispose synchronously, but not for grpc.core
	
	readonly ChannelCache                                       _channelCache;
	readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;
	readonly CancellationTokenSource                            _cts;

	readonly Dictionary<string, Func<RpcException, Exception>> _exceptionMap;
	readonly Lazy<HttpFallback>                                _httpFallback;

	/// Constructs a new <see cref="EventStoreClientBase"/>.
	protected EventStoreClientBase(EventStoreClientSettings? settings, Dictionary<string, Func<RpcException, Exception>> exceptionMap) {
		Settings      = settings ?? new EventStoreClientSettings();
		_exceptionMap = exceptionMap;
		_cts          = new CancellationTokenSource();
		_channelCache = new ChannelCache(Settings);
		_httpFallback = new Lazy<HttpFallback>(() => new HttpFallback(Settings));

		ConnectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

		var channelSelector = new ChannelSelector(Settings, _channelCache);
		
		_channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
			(endPoint, onBroken) => GetChannelInfoExpensive(endPoint, onBroken, channelSelector, _cts.Token),
			Settings.ConnectivitySettings.DiscoveryInterval,
			ReconnectionRequired.Rediscover.Instance,
			Settings.LoggerFactory
		);
	}

	/// The name of the connection.
	public string ConnectionName { get; }

	/// The <see cref="EventStoreClientSettings"/>.
	protected EventStoreClientSettings Settings { get; }

	/// <inheritdoc />
	public virtual async ValueTask DisposeAsync() {
		_channelInfoProvider.Dispose();
		_cts.Cancel();
		_cts.Dispose();
		
		await _channelCache.DisposeAsync().ConfigureAwait(false);

		if (_httpFallback.IsValueCreated) _httpFallback.Value.Dispose();
	}

	/// <inheritdoc />
	public virtual void Dispose() {
		_channelInfoProvider.Dispose();
		_cts.Cancel();
		_cts.Dispose();
		_channelCache.Dispose();

		if (_httpFallback.IsValueCreated) _httpFallback.Value.Dispose();
	}

	// Select a channel and query its capabilities. This is an expensive call that
	// we don't want to do often.
	async Task<ChannelInfo> GetChannelInfoExpensive(
		ReconnectionRequired reconnectionRequired,
		Action<ReconnectionRequired> onReconnectionRequired,
		IChannelSelector channelSelector,
		CancellationToken cancellationToken
	) {
		var channel = reconnectionRequired switch {
			ReconnectionRequired.Rediscover               => await channelSelector.SelectChannelAsync(cancellationToken).ConfigureAwait(false),
			ReconnectionRequired.NewLeader (var endPoint) => channelSelector.SelectChannel(endPoint),
			_                                             => throw new ArgumentException(null, nameof(reconnectionRequired))
		};

		var invoker = channel.CreateCallInvoker()
			.Intercept(new TypedExceptionInterceptor(_exceptionMap))
			.Intercept(new ConnectionNameInterceptor(ConnectionName))
			.Intercept(new ReportLeaderInterceptor(onReconnectionRequired));

		if (Settings.Interceptors is not null)
			foreach (var interceptor in Settings.Interceptors)
				invoker = invoker.Intercept(interceptor);

		var caps = await new GrpcServerCapabilitiesClient(Settings)
			.GetAsync(invoker, cancellationToken)
			.ConfigureAwait(false);

		return new ChannelInfo(channel, caps, invoker);
	}

	/// Gets the current channel info.
	protected async ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken) =>
		await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Only exists so that we can manually trigger rediscovery in the tests
	/// in cases where the server doesn't yet let the client know that it needs to.
	/// note if rediscovery is already in progress it will continue, not restart.
	/// </summary>
	internal Task RediscoverAsync() {
		_channelInfoProvider.Reset();
		return Task.CompletedTask;
	}

	/// Returns the result of an HTTP Get request based on the client settings.
	protected async Task<T> HttpGet<T>(
		string path, Action onNotFound, ChannelInfo channelInfo,
		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
	) =>
		await _httpFallback.Value
			.HttpGetAsync<T>(path, channelInfo, deadline, userCredentials, onNotFound, cancellationToken)
			.ConfigureAwait(false);

	/// Executes an HTTP Post request based on the client settings.
	protected async Task HttpPost(
		string path, string query, Action onNotFound, ChannelInfo channelInfo,
		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
	) {
		await _httpFallback.Value
			.HttpPostAsync(
				path, query, channelInfo, deadline, userCredentials, onNotFound,
				cancellationToken
			)
			.ConfigureAwait(false);
	}

	/// Returns an InvalidOperation exception.
	protected static Exception InvalidOption<T>(T option) where T : Enum =>
		new InvalidOperationException($"The {typeof(T).Name} {option:x} was not valid.");
}