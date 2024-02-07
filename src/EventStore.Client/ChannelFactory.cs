using System.Net.Http;
using Grpc.Net.Client;
using System.Net.Security;
using EndPoint = System.Net.EndPoint;
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
#if NET
				HttpClient = new HttpClient(CreateHandler(), true) {
					Timeout = System.Threading.Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0)
				},
#else
				HttpHandler = CreateHandler(),
#endif
				LoggerFactory         = settings.LoggerFactory,
				Credentials           = settings.ChannelCredentials,
				DisposeHttpClient     = true,
				MaxReceiveMessageSize = MaxReceiveMessageLength
			});

			HttpMessageHandler CreateHandler() {
				if (settings.CreateHttpMessageHandler != null) {
					return settings.CreateHttpMessageHandler.Invoke();
				}
#if NET
				var handler = new SocketsHttpHandler {
					KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true,
				};

				var sslOptions = new SslClientAuthenticationOptions();

				if (settings.ConnectivitySettings is { TlsCaFile: not null, Insecure: false }) {
					sslOptions.ClientCertificates?.Add(settings.ConnectivitySettings.TlsCaFile);
				}

				if (!settings.ConnectivitySettings.TlsVerifyCert) {
					sslOptions.RemoteCertificateValidationCallback = delegate { return true; };
				}

				handler.SslOptions = sslOptions;
#else
				var handler = new WinHttpHandler {
					TcpKeepAliveEnabled = true,
					TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
					TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
					EnableMultipleHttp2Connections = true
				};

				if (settings.ConnectivitySettings is { TlsCaFile: not null, Insecure: false }) {
					handler.ClientCertificates.Add(settings.ConnectivitySettings.TlsCaFile);
				}

				if (!settings.ConnectivitySettings.TlsVerifyCert) {
					handler.ServerCertificateValidationCallback = delegate { return true; };
				}
#endif
				return handler;
			}
		}
	}
}
