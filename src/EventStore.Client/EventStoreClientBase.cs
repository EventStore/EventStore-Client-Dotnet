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
	public abstract class EventStoreClientBase : IDisposable {
		private readonly GrpcChannel _channel;
		private readonly HttpMessageHandler _httpHandler;

		protected CallInvoker CallInvoker { get; }
		protected EventStoreClientSettings Settings { get; }

		protected EventStoreClientBase(EventStoreClientSettings? settings,
			IDictionary<string, Func<RpcException, Exception>> exceptionMap) {
			Settings = settings ?? new EventStoreClientSettings();
			if(Settings.ConnectivitySettings.Address.Scheme == Uri.UriSchemeHttp || !Settings.ConnectivitySettings.UseHttps){
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			_httpHandler = Settings.CreateHttpMessageHandler?.Invoke() ?? new HttpClientHandler();

			var connectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";
			Action<Exception>? exceptionNotificationHook = null;

			if (Settings.ConnectivitySettings.GossipSeeds.Length > 0) {
				_httpHandler = ClusterAwareHttpHandler.Create(Settings, _httpHandler);
			}

			_channel = GrpcChannel.ForAddress(Settings.ConnectivitySettings.Address, new GrpcChannelOptions {
				HttpClient = new HttpClient(_httpHandler) {
					Timeout = Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
				},
				LoggerFactory = Settings.LoggerFactory,
				Credentials = Settings.ChannelCredentials
			});

			CallInvoker = (Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				_channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(exceptionMap, exceptionNotificationHook))
					.Intercept(new ConnectionNameInterceptor(connectionName)),
				(invoker, interceptor) => invoker.Intercept(interceptor));
		}

		public void Dispose() {
			_channel?.Dispose();
			_httpHandler?.Dispose();
		}
	}
}
