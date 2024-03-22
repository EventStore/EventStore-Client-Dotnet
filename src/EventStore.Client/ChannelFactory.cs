using System.Net.Http;
using Grpc.Net.Client;
using TChannel = Grpc.Net.Client.GrpcChannel;

namespace EventStore.Client {
	internal static class ChannelFactory {
		private const int MaxReceiveMessageLength = 17 * 1024 * 1024;

		public static TChannel CreateChannel(EventStoreClientSettings settings, ChannelIdentifier channelIdentifier) {
			var address = channelIdentifier.DnsEndpoint.ToUri(!settings.ConnectivitySettings.Insecure);

			if (settings.ConnectivitySettings.Insecure) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return TChannel.ForAddress(
				address,
				new GrpcChannelOptions {
#if NET
					HttpClient = new HttpClient(CreateHandler(), true) {
						Timeout               = System.Threading.Timeout.InfiniteTimeSpan,
						DefaultRequestVersion = new Version(2, 0)
					},
#else
					HttpHandler = CreateHandler(),
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

				bool configureClientCert = settings.ConnectivitySettings.UserCertificate != null
				                        || settings.ConnectivitySettings.TlsCaFile != null
				                        || channelIdentifier.UserCertificate != null;

				var certificate = channelIdentifier?.UserCertificate
				               ?? settings.ConnectivitySettings.UserCertificate
				               ?? settings.ConnectivitySettings.TlsCaFile;

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
					handler.SslOptions.ClientCertificates = [certificate!];
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
