using System;
using EndPoint = System.Net.EndPoint;
using System.Net.Http;
using Grpc.Net.Client;
using TChannel = Grpc.Net.Client.GrpcChannel;

namespace EventStore.Client {
	internal static class ChannelFactory {
		private const int MaxReceiveMessageLength = 17 * 1024 * 1024;

		public static TChannel CreateChannel(EventStoreClientSettings settings, EndPoint endPoint) {
			var address = endPoint.ToUri(!settings.ConnectivitySettings.Insecure);

			if (settings.ConnectivitySettings.Insecure) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return TChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(CreateHandler(), true) {
					Timeout = System.Threading.Timeout.InfiniteTimeSpan,
#if NET
					DefaultRequestVersion = new Version(2, 0),
#endif
				},
				LoggerFactory = settings.LoggerFactory,
				Credentials = settings.ChannelCredentials,
				DisposeHttpClient = true,
				MaxReceiveMessageSize = MaxReceiveMessageLength
			});

			HttpMessageHandler CreateHandler() {
				if (settings.CreateHttpMessageHandler != null) {
					return settings.CreateHttpMessageHandler.Invoke();
				}

#if NET
				return new SocketsHttpHandler {
					KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true
				};
#else
                return new WinHttpHandler {
                    TcpKeepAliveEnabled = true,
                    TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
                    TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
                    EnableMultipleHttp2Connections = true
                };
#endif
			}
		}
	}
}
