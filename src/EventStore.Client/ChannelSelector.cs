using System.Threading;
using System.Threading.Tasks;
using EndPoint = System.Net.EndPoint;

#nullable enable
namespace EventStore.Client {
	internal class ChannelSelector : IChannelSelector {
		private readonly CancellationToken _cancellationToken;
		private readonly IChannelSelector _inner;

		public ChannelSelector(EventStoreClientSettings settings, CancellationToken cancellationToken) {
			_cancellationToken = cancellationToken;
			_inner = settings.ConnectivitySettings.IsSingleNode
				? new SingleNodeChannelSelector(settings, new GrpcServerCapabilitiesClient(settings), cancellationToken)
				: new GossipChannelSelector(settings, new GrpcGossipClient(),
					new GrpcServerCapabilitiesClient(settings),
					cancellationToken);
		}

		public Task<ChannelInfo> SelectChannel(CancellationToken cancellationToken) =>
			_inner.SelectChannel(cancellationToken);

		public void SetEndPoint(EndPoint leader) => _inner.SetEndPoint(leader);

		public void Rediscover() => _inner.Rediscover();

		public ValueTask DisposeAsync() => _inner.DisposeAsync();
	}
}
