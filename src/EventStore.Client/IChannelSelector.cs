using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal interface IChannelSelector {
		// Let the channel selector pick an endpoint.
		Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken);

		// Get a channel for the specified endpoint
		ChannelBase SelectChannel(DnsEndPoint endPoint);
	}
}
