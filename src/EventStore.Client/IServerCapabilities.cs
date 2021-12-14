using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal interface IServerCapabilities {
		public ValueTask<ServerCapabilitiesInfo> GetServerCapabilities(ChannelInfo channelInfo,
			CancellationToken cancellationToken = default);

		public void Reset(ChannelInfo channelInfo);
	}
}
