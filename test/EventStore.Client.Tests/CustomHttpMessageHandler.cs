namespace EventStore.Client.Tests;

using System.Net.Http;

internal static class CustomHttpMessageHandler {
	internal static Func<HttpMessageHandler>? CreateDefaultHandler(EventStoreClientSettings settings) {
		return () => {
			bool configureClientCert = settings.ConnectivitySettings.UserCertificate != null
			                        || settings.ConnectivitySettings.TlsCaFile != null;

			var certificate = settings.ConnectivitySettings.UserCertificate
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
			if (!settings.ConnectivitySettings.TlsVerifyCert) {
				handler.ServerCertificateValidationCallback = delegate { return true; };
			}
#endif

			return handler;
		};
	}
}
