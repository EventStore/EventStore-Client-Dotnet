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

			var grpcChannelOptions =
				new GrpcChannelOptions {
					LoggerFactory         = settings.LoggerFactory,
					Credentials           = settings.ChannelCredentials,
					DisposeHttpClient     = true,
					MaxReceiveMessageSize = MaxReceiveMessageLength
				};

			var httpMessageHandler = settings.CreateHttpMessageHandler?.Invoke()!;
#if NET
			grpcChannelOptions.HttpClient = new HttpClient(
				httpMessageHandler,
				true
			) {
				Timeout               = System.Threading.Timeout.InfiniteTimeSpan,
				DefaultRequestVersion = new Version(2, 0),
			};
#else
			grpcChannelOptions.HttpHandler = httpMessageHandler;
#endif

			return TChannel.ForAddress(
				address,
				grpcChannelOptions
			);
		}
	}
}
