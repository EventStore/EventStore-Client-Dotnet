using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class SingleNodeChannelSelector : IChannelSelector {
		private readonly ChannelCache _channelCache;
		private readonly DnsEndPoint _endPoint;
		private readonly bool _https;

		public SingleNodeChannelSelector(
			EventStoreClientSettings settings,
			ChannelCache channelCache) {

			_channelCache = channelCache;
			var uri = settings.ConnectivitySettings.Address;
			_endPoint = new DnsEndPoint(host: uri.Host, port: uri.Port);
			_https = string.Compare(uri.Scheme, Uri.UriSchemeHttps, ignoreCase: true) == 0;
		}

		public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
			Task.FromResult(SelectChannel(_endPoint));

		public ChannelBase SelectChannel(DnsEndPoint endPoint) =>
			_channelCache.GetChannelInfo(endPoint, _https);
	}
}
