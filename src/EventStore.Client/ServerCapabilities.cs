using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class ServerCapabilities : IServerCapabilities {
		private readonly IServerCapabilitiesClient _serverCapabilitiesClient;
		private readonly Dictionary<EndPoint, ServerCapabilitiesInfo> _cache;

		public ServerCapabilities(IServerCapabilitiesClient serverCapabilitiesClient) {
			_serverCapabilitiesClient = serverCapabilitiesClient;
			_cache = new Dictionary<EndPoint, ServerCapabilitiesInfo>();
		}

		public async ValueTask<ServerCapabilitiesInfo> GetServerCapabilities(ChannelInfo channelInfo,
			CancellationToken cancellationToken = default) {
			if (!_cache.TryGetValue(channelInfo.EndPoint, out var serverCapabilities)) {
				serverCapabilities = await _serverCapabilitiesClient.GetAsync(channelInfo.Channel, cancellationToken)
					.ConfigureAwait(false);
				_cache.Add(channelInfo.EndPoint, serverCapabilities);
			}

			return serverCapabilities;
		}

		public void Reset(ChannelInfo channelInfo) => _cache.Remove(channelInfo.EndPoint);
	}
}
