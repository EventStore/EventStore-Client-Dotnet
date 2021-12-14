using System.Net;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	internal class ChannelSelector : IChannelSelector {
		private readonly CancellationToken _cancellationToken;
		private readonly IChannelSelector _inner;

		public ChannelSelector(EventStoreClientSettings settings, CancellationToken cancellationToken) {
			_cancellationToken = cancellationToken;
			_inner = settings.ConnectivitySettings.IsSingleNode
				? new SingleNodeChannelSelector(settings)
				: new GossipChannelSelector(settings, new GrpcGossipClient(),
					cancellationToken);
		}

		public ValueTask<ChannelInfo> SelectChannel(CancellationToken cancellationToken) =>
			_inner.SelectChannel(cancellationToken);

		public void Rediscover(DnsEndPoint? leader) => _inner.Rediscover(leader);

		public ValueTask DisposeAsync() => _inner.DisposeAsync();
	}
}
