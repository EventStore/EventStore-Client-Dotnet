using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal class ChannelSelector : IChannelSelector {
		private readonly IChannelSelector _inner;

		public ChannelSelector(
			EventStoreClientSettings settings,
			ChannelCache channelCache) {
			_inner = settings.ConnectivitySettings.IsSingleNode
				? new SingleNodeChannelSelector(settings, channelCache)
				: new GossipChannelSelector(settings, channelCache, new GrpcGossipClient());
		}

		public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
			_inner.SelectChannelAsync(cancellationToken);

		public ChannelBase SelectChannel(DnsEndPoint endPoint) =>
			_inner.SelectChannel(endPoint);
	}
}
