using System.Security.Cryptography.X509Certificates;
using Grpc.Core;

namespace EventStore.Client {
	internal class ChannelSelector(
		EventStoreClientSettings settings,
		ChannelCache channelCache
	)
		: IChannelSelector {
		private readonly IChannelSelector _inner = settings.ConnectivitySettings.IsSingleNode
			? new SingleNodeChannelSelector(settings, channelCache)
			: new GossipChannelSelector(settings, channelCache, new GrpcGossipClient(settings));

		public Task<ChannelBase> SelectChannelAsync(X509Certificate2? userCertificate, CancellationToken cancellationToken) {
			return _inner.SelectChannelAsync(userCertificate, cancellationToken);
		}

		public ChannelBase SelectChannel(ChannelIdentifier channelIdentifier) =>
			_inner.SelectChannel(channelIdentifier);
	}
}
