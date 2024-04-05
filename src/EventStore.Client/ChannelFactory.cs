using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
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

			return TChannel.ForAddress(
				address,
				new GrpcChannelOptions {
#if NET48
					HttpHandler = CreateHandler(),
#else
					HttpClient = new HttpClient(CreateHandler(), true) {
						Timeout               = System.Threading.Timeout.InfiniteTimeSpan,
						DefaultRequestVersion = new Version(2, 0)
					},
#endif
					LoggerFactory         = settings.LoggerFactory,
					Credentials           = settings.ChannelCredentials,
					DisposeHttpClient     = true,
					MaxReceiveMessageSize = MaxReceiveMessageLength
				}
			);

			HttpMessageHandler CreateHandler() {
				if (settings.CreateHttpMessageHandler != null) {
					return settings.CreateHttpMessageHandler.Invoke();
				}

				var certificate = settings.ConnectivitySettings.ClientCertificate ??
				                  settings.ConnectivitySettings.TlsCaFile;

				var configureClientCert = settings.ConnectivitySettings is { Insecure: false } && certificate != null;
#if NET
				var handler = new SocketsHttpHandler {
					KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true,
				};
#else
				var handler = new WinHttpHandler {
					TcpKeepAliveEnabled = true,
					TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
					TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
					EnableMultipleHttp2Connections = true
				};
#endif
				if (settings.ConnectivitySettings.Insecure) return handler;
#if NET
				if (configureClientCert) {
					handler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate! };
				}

				if (!settings.ConnectivitySettings.TlsVerifyCert) {
					handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
				}
#else
				if (configureClientCert) {
					handler.ClientCertificates.Add(certificate!);
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
