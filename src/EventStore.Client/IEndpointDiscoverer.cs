using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	/// <summary>
	/// An interface used to discover an <see cref="EndPoint"/> used to communicate with EventStoreDB.
	/// </summary>
	public interface IEndpointDiscoverer {
		/// <summary>
		/// Discovers the <see cref="EndPoint"/> used to communicate with EventStoreDB.
		/// </summary>
		/// <returns></returns>
		Task<EndPoint> DiscoverAsync(CancellationToken cancellationToken = default);
	}
}
