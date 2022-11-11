using System;
using EndPoint = System.Net.EndPoint;
using System.Net.Http;
using Grpc.Net.Client;
using TChannel = Grpc.Net.Client.GrpcChannel;

namespace EventStore.Client {
	internal static class ChannelFactory {
		private const int MaxReceiveMessageLength = 17 * 1024 * 1024;

		public static TChannel CreateChannel(EventStoreClientSettings settings, EndPoint endPoint, bool https) =>
			CreateChannel(settings, endPoint.ToUri(https));

		public static TChannel CreateChannel(EventStoreClientSettings settings, Uri? address) {
			address ??= settings.ConnectivitySettings.Address;

			if (address.Scheme == Uri.UriSchemeHttp ||settings.ConnectivitySettings.Insecure) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return TChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(CreateHandler(), true) {
					Timeout = System.Threading.Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
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

				return new SocketsHttpHandler {
					KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true
				};
			}
		}
	}
}
