using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class SingleNodeChannelSelector : IChannelSelector {
		private readonly ChannelBase _channel;
		private readonly Lazy<Task<ServerCapabilities>> _serverCapabilitiesLazy;
		private Task<ServerCapabilities> ServerCapabilities => _serverCapabilitiesLazy.Value;

		public SingleNodeChannelSelector(EventStoreClientSettings settings,
			IServerCapabilitiesClient serverCapabilities,
			CancellationToken cancellationToken) {
			_channel = ChannelFactory.CreateChannel(settings, settings.ConnectivitySettings.Address);
			_serverCapabilitiesLazy =
				new Lazy<Task<ServerCapabilities>>(async () => await serverCapabilities.GetAsync(_channel,
					cancellationToken).ConfigureAwait(false), LazyThreadSafetyMode.PublicationOnly);
		}

		public ValueTask DisposeAsync() => _channel.DisposeAsync();

		public async Task<ChannelInfo> SelectChannel(CancellationToken cancellationToken) =>
			new(_channel, await ServerCapabilities.ConfigureAwait(false));

		public void SetEndPoint(EndPoint leader) {
		}

		public void Rediscover() {
		}
	}
}
