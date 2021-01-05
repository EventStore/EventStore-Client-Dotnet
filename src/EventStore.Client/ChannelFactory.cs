using System;
using System.Net;
using Grpc.Core;
#if !GRPC_CORE
using System.Net.Http;
using System.Threading;
using Grpc.Net.Client;
#endif
#nullable enable
namespace EventStore.Client {
	internal static class ChannelFactory {
		public static ChannelBase CreateChannel(EventStoreClientSettings settings, EndPoint endPoint) =>
			CreateChannel(settings, endPoint.ToUri(settings.ConnectivitySettings.GossipOverHttps));

		public static ChannelBase CreateChannel(EventStoreClientSettings settings, Uri? address) {
			address ??= settings.ConnectivitySettings.Address;

#if !GRPC_CORE
			if (address.Scheme == Uri.UriSchemeHttp ||
			    !settings.ConnectivitySettings.GossipOverHttps) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}
			return GrpcChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(new ClusterAwareHttpHandler(settings.ConnectivitySettings.GossipOverHttps,
					settings.ConnectivitySettings.NodePreference == NodePreference.Leader,
					settings.ConnectivitySettings.IsSingleNode
						? (IEndpointDiscoverer)new SingleNodeEndpointDiscoverer(settings.ConnectivitySettings.Address)
						: new GossipBasedEndpointDiscoverer(settings.ConnectivitySettings,
							new GrpcGossipClient(settings))) {
					InnerHandler = settings.CreateHttpMessageHandler?.Invoke() ?? new SocketsHttpHandler()
				}, true) {
					Timeout = Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
				},
				LoggerFactory = settings.LoggerFactory,
				Credentials = settings.ChannelCredentials,
				DisposeHttpClient = true
			});
#else
			return new Channel(address.Host, address.Port, settings.ChannelCredentials ?? ChannelCredentials.Insecure);
#endif
		}
	}
}
