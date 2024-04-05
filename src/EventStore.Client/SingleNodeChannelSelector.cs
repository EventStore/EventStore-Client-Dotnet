using System.Net;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client;

class SingleNodeChannelSelector : IChannelSelector {
	readonly ChannelCache _channelCache;
	readonly DnsEndPoint  _endPoint;
	readonly ILogger      _log;

	public SingleNodeChannelSelector(EventStoreClientSettings settings, ChannelCache channelCache) {
		_log = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ??
		       new NullLogger<SingleNodeChannelSelector>();

		_channelCache = channelCache;

		var uri = settings.ConnectivitySettings.ResolvedAddressOrDefault;
		_endPoint = new DnsEndPoint(uri.Host, uri.Port);
	}

	public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
		Task.FromResult(SelectChannel(_endPoint));

	public ChannelBase SelectChannel(DnsEndPoint endPoint) {
		_log.LogInformation("Selected {endPoint}.", endPoint);

		return _channelCache.GetChannelInfo(endPoint);
	}
}