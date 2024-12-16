using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal interface IGossipClient {
		public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel,
			CancellationToken cancellationToken);
	}
}
