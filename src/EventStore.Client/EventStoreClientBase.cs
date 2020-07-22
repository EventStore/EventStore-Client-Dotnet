using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The base class used by clients used to communicate with the EventStoreDB.
	/// </summary>
	public abstract class EventStoreClientBase : IDisposable {
		private readonly GrpcChannel _channel;
		private readonly HttpMessageHandler _httpHandler;
		private readonly HttpMessageHandler _innerHttpHandler;

		/// <summary>
		/// The <see cref="CallInvoker"/>.
		/// </summary>
		protected CallInvoker CallInvoker { get; }

		/// <summary>
		/// The <see cref="EventStoreClientSettings"/>.
		/// </summary>
		protected EventStoreClientSettings Settings { get; }

		/// <summary>
		/// Constructs a new <see cref="EventStoreClientBase"/>.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="exceptionMap"></param>
		protected EventStoreClientBase(EventStoreClientSettings? settings,
			IDictionary<string, Func<RpcException, Exception>> exceptionMap) {
			Settings = settings ?? new EventStoreClientSettings();

			var connectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

			if (Settings.ConnectivitySettings.Address.Scheme == Uri.UriSchemeHttp ||
			    !Settings.ConnectivitySettings.GossipOverHttps) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

#if NETCOREAPP3_1
			_innerHttpHandler = Settings.CreateHttpMessageHandler?.Invoke() ?? new SocketsHttpHandler();
#elif NETSTANDARD2_1
			_innerHttpHandler = Settings.CreateHttpMessageHandler?.Invoke() ?? new HttpClientHandler();
#endif

			_httpHandler = Settings.ConnectivitySettings.IsSingleNode
				? (HttpMessageHandler)new SingleNodeHttpHandler(Settings) {
					InnerHandler = _innerHttpHandler
				}
				: ClusterAwareHttpHandler.Create(Settings, _innerHttpHandler);

#if NETSTANDARD2_1
			_httpHandler = new DefaultRequestVersionHandler(_httpHandler);
#endif

			_channel = GrpcChannel.ForAddress(new UriBuilder(Settings.ConnectivitySettings.Address) {
				Scheme = Uri.UriSchemeHttps
			}.Uri, new GrpcChannelOptions {
				HttpClient = new HttpClient(_httpHandler) {
					Timeout = Timeout.InfiniteTimeSpan,
#if NETCOREAPP3_1
					DefaultRequestVersion = new Version(2, 0),
#endif
				},
				LoggerFactory = Settings.LoggerFactory,
				Credentials = Settings.ChannelCredentials
			});

			Action<Exception>? exceptionNotificationHook =
				_httpHandler is ClusterAwareHttpHandler h
					? h.ExceptionOccurred
					: new Action<Exception>(ex => { });

			CallInvoker = (Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				_channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(exceptionMap, exceptionNotificationHook))
					.Intercept(new ConnectionNameInterceptor(connectionName)),
				(invoker, interceptor) => invoker.Intercept(interceptor));
		}

		/// <inheritdoc />
		public void Dispose() {
			_channel?.Dispose();
			_innerHttpHandler?.Dispose();
			_httpHandler?.Dispose();
		}
	}
}
