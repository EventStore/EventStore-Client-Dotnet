using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal class SingleNodeChannelSelector : IChannelSelector {
		private readonly ChannelBase _channel;
		private readonly ChannelInfo _channelInfo;

		public SingleNodeChannelSelector(EventStoreClientSettings settings) {
			_channel = ChannelFactory.CreateChannel(settings, settings.ConnectivitySettings.Address);
			_channelInfo = new ChannelInfo(_channel, new DnsEndPoint(settings.ConnectivitySettings.Address.Host,
					settings.ConnectivitySettings.Address.Port));
		}

		public ValueTask DisposeAsync() => _channel.DisposeAsync();

		public ValueTask<ChannelInfo> SelectChannel(CancellationToken cancellationToken) => new(_channelInfo);

		public void Rediscover(DnsEndPoint? leader) {
		}
	}
}
