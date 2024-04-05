using System.Net;
using Grpc.Core;

namespace EventStore.Client;

class ChannelSelector(EventStoreClientSettings settings, ChannelCache channelCache) : IChannelSelector {
	readonly IChannelSelector _inner = settings.ConnectivitySettings.IsSingleNode
		? new SingleNodeChannelSelector(settings, channelCache)
		: new GossipChannelSelector(settings, channelCache, new GrpcGossipClient(settings));

	public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
		_inner.SelectChannelAsync(cancellationToken);

	public ChannelBase SelectChannel(DnsEndPoint endPoint) =>
		_inner.SelectChannel(endPoint);
}