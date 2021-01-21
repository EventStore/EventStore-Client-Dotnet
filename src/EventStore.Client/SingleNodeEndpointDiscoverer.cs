using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class SingleNodeEndpointDiscoverer : IEndpointDiscoverer {
		private readonly Task<EndPoint> _endPoint;

		public SingleNodeEndpointDiscoverer(Uri address) {
			_endPoint = Task.FromResult<EndPoint>(new DnsEndPoint(address.Host, address.Port));
		}

		public Task<EndPoint> DiscoverAsync(CancellationToken cancellationToken = default) => _endPoint;
	}
}
