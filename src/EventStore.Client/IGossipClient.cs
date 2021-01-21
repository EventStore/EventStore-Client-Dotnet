using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal interface IGossipClient {
		public ValueTask<ClusterMessages.ClusterInfo> GetAsync(EndPoint endPoint,
			CancellationToken cancellationToken = default);
	}
}
