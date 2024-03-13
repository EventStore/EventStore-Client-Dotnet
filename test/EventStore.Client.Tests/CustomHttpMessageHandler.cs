namespace EventStore.Client.Tests;

using System.Net.Http;

internal static class CustomHttpMessageHandler {
	internal static Func<HttpMessageHandler>? CreateDefaultHandler(EventStoreClientSettings settings) {
		return () => {
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
			return handler;
		};
	}
}
