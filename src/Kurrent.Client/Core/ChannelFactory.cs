using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using EndPoint = System.Net.EndPoint;
using TChannel = Grpc.Net.Client.GrpcChannel;

namespace EventStore.Client {

	internal static class ChannelFactory {
		private const int MaxReceiveMessageLength = 17 * 1024 * 1024;

		public static TChannel CreateChannel(KurrentClientSettings settings, EndPoint endPoint) {
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
		static HttpMessageHandler CreateHandler(KurrentClientSettings settings) {
			if (settings.CreateHttpMessageHandler is not null)
				return settings.CreateHttpMessageHandler.Invoke();

			var handler = new WinHttpHandler {
				TcpKeepAliveEnabled = true,
				TcpKeepAliveTime = settings.ConnectivitySettings.KeepAliveTimeout,
				TcpKeepAliveInterval = settings.ConnectivitySettings.KeepAliveInterval,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure) return handler;

			if (settings.ConnectivitySettings.ClientCertificate is not null)
				handler.ClientCertificates.Add(settings.ConnectivitySettings.ClientCertificate);

			handler.ServerCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
				false => delegate { return true; },
				true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
					if (chain is null) return false;

					chain.ChainPolicy.ExtraStore.Add(settings.ConnectivitySettings.TlsCaFile);
					return chain.Build(certificate);
				},
				_ => null
			};

			return handler;
		}
#else
		static HttpMessageHandler CreateHandler(KurrentClientSettings settings) {
			if (settings.CreateHttpMessageHandler is not null)
				return settings.CreateHttpMessageHandler.Invoke();

			var handler = new SocketsHttpHandler {
				KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
				KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure)
				return handler;

			if (settings.ConnectivitySettings.ClientCertificate is not null) {
				handler.SslOptions.ClientCertificates = new X509CertificateCollection {
					settings.ConnectivitySettings.ClientCertificate
				};
			}

			handler.SslOptions.RemoteCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
				false => delegate { return true; },
				true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
					if (certificate is not X509Certificate2 peerCertificate || chain is null) return false;

					chain.ChainPolicy.TrustMode                   = X509ChainTrustMode.CustomRootTrust;
					chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);
					return chain.Build(peerCertificate);
				},
				_ => null
			};

			return handler;
		}
#endif
		}
	}
}
