using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal interface IChannelSelector {
		// Let the channel selector pick an endpoint.
		Task<ChannelBase> SelectChannelAsync(X509Certificate2? userCertificate, CancellationToken cancellationToken);

		// Get a channel for the specified endpoint
		ChannelBase SelectChannel(ChannelIdentifier channelIdentifier);
	}
}
