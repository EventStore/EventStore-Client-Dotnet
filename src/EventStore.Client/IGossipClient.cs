using Grpc.Core;

namespace EventStore.Client;

interface IGossipClient {
	public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel, CancellationToken cancellationToken);
}