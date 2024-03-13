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
		private readonly ChannelIdentifier _channelIdentifier;

		public SingleNodeChannelSelector(
			EventStoreClientSettings settings,
			ChannelCache channelCache) {

			_log = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ??
				new NullLogger<SingleNodeChannelSelector>();

			_channelCache = channelCache;

			var uri             = settings.ConnectivitySettings.ResolvedAddressOrDefault;

			_channelIdentifier = new ChannelIdentifier(new DnsEndPoint(uri.Host, uri.Port));
		}

		public Task<ChannelBase> SelectChannelAsync(
			UserCredentials? userCredentials, CancellationToken cancellationToken
		) =>
			Task.FromResult(SelectChannel(_channelIdentifier));

		public ChannelBase SelectChannel(ChannelIdentifier channelIdentifier) {
			_log.LogInformation("Selected {endPoint}.", channelIdentifier);

			return _channelCache.GetChannelInfo(channelIdentifier);
		}
	}
}
