using System;
using System.Net;
using EndPoint = System.Net.EndPoint;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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

				var handler = new SocketsHttpHandler {
					KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true
				};

				if (!settings.ConnectivitySettings.TlsVerifyCert) {
					handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
				}

				return handler;
			}
		}
	}
}
