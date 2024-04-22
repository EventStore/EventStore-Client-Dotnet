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
					HttpHandler = CreateHandler(settings),
#else
					HttpClient = new HttpClient(CreateHandler(settings), true) {
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


#if NET48
		static HttpMessageHandler CreateHandler(EventStoreClientSettings settings) {
			if (settings.CreateHttpMessageHandler != null) {
				return settings.CreateHttpMessageHandler.Invoke();
			}

			var certificate = settings.ConnectivitySettings.ClientCertificate ??
			                  settings.ConnectivitySettings.TlsCaFile;

			var configureClientCert = settings.ConnectivitySettings is { Insecure: false } && certificate != null;

			var handler = new WinHttpHandler {
				TcpKeepAliveEnabled = true,
				TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
				TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure) return handler;

			if (configureClientCert) {
				handler.ClientCertificates.Add(certificate!);
			}

			if (!settings.ConnectivitySettings.TlsVerifyCert) {
				handler.ServerCertificateValidationCallback = delegate { return true; };
			}

			return handler;
		}
#else
		static HttpMessageHandler CreateHandler(EventStoreClientSettings settings) {
			if (settings.CreateHttpMessageHandler != null) {
				return settings.CreateHttpMessageHandler.Invoke();
			}

			var certificate = settings.ConnectivitySettings.ClientCertificate ??
			                  settings.ConnectivitySettings.TlsCaFile;

			var configureClientCert = settings.ConnectivitySettings is { Insecure: false } && certificate != null;

			var handler = new SocketsHttpHandler {
				KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
				KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
				EnableMultipleHttp2Connections = true,
			};

			if (settings.ConnectivitySettings.Insecure) return handler;

			if (configureClientCert) {
				handler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate! };
			}

			if (!settings.ConnectivitySettings.TlsVerifyCert) {
				handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
			}

			return handler;
		}
#endif
		}
	}
}
