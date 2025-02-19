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

		public SingleNodeChannelSelector(
			KurrentClientSettings settings,
			ChannelCache channelCache) {

			_log = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ??
				new NullLogger<SingleNodeChannelSelector>();

			_channelCache = channelCache;
			
			var uri = settings.ConnectivitySettings.ResolvedAddressOrDefault;
			_endPoint = new DnsEndPoint(host: uri.Host, port: uri.Port);
		}

		public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
			Task.FromResult(SelectChannel(_endPoint));

		public ChannelBase SelectChannel(DnsEndPoint endPoint) {
			_log.LogInformation("Selected {endPoint}.", endPoint);

			return _channelCache.GetChannelInfo(endPoint);
		}
	}
}
