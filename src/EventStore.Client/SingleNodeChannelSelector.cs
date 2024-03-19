using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventStore.Client
{
    internal class SingleNodeChannelSelector : IChannelSelector
    {
        private readonly ILogger _log;
        private readonly ChannelCache _channelCache;
        private readonly EventStoreClientSettings _settings;

        public SingleNodeChannelSelector(EventStoreClientSettings settings, ChannelCache channelCache)
        {
            _log = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ?? new NullLogger<SingleNodeChannelSelector>();
            _settings = settings;
            _channelCache = channelCache;
        }

        public Task<ChannelBase> SelectChannelAsync(X509Certificate2? userCertificate, CancellationToken cancellationToken)
        {
            var uri = _settings.ConnectivitySettings.ResolvedAddressOrDefault;
            var channelIdentifier = new ChannelIdentifier(new DnsEndPoint(uri.Host, uri.Port), userCertificate);
            return Task.FromResult(SelectChannel(channelIdentifier));
        }

        public ChannelBase SelectChannel(ChannelIdentifier channelIdentifier)
        {
            _log.LogInformation("Selected {endPoint}.", channelIdentifier);
            return _channelCache.GetChannelInfo(channelIdentifier);
        }
    }
}
