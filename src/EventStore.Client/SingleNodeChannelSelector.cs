using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client {
	internal class SingleNodeChannelSelector : IChannelSelector {
		private readonly ILogger _log;
		private readonly ChannelCache _channelCache;
		private readonly DnsEndPoint _endPoint;
		private readonly bool _https;

		public SingleNodeChannelSelector(
			EventStoreClientSettings settings,
			ChannelCache channelCache) {

			_log = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ??
				new NullLogger<SingleNodeChannelSelector>();

			_channelCache = channelCache;
			var uri = settings.ConnectivitySettings.Address;
			_endPoint = new DnsEndPoint(host: uri.Host, port: uri.Port);
			_https = string.Compare(uri.Scheme, Uri.UriSchemeHttps, ignoreCase: true) == 0;
		}

		public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
			Task.FromResult(SelectChannel(_endPoint));

		public ChannelBase SelectChannel(DnsEndPoint endPoint) {
			_log.LogInformation("Selected {endPoint}.", endPoint);

			return _channelCache.GetChannelInfo(endPoint, _https);
		}
	}
}
