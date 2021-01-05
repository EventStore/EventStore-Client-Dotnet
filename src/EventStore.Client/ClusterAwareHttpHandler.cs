using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	/// <inheritdoc />
	internal class ClusterAwareHttpHandler : DelegatingHandler {
		private readonly bool _useHttps;
		private readonly bool _requiresLeader;
		private readonly IEndpointDiscoverer _endpointDiscoverer;
		private Lazy<Task<EndPoint>> _endpoint;

		public ClusterAwareHttpHandler(bool useHttps, bool requiresLeader, IEndpointDiscoverer endpointDiscoverer) {
			_useHttps = useHttps;
			_requiresLeader = requiresLeader;
			_endpointDiscoverer = endpointDiscoverer;
			_endpoint = new Lazy<Task<EndPoint>>(() => endpointDiscoverer.DiscoverAsync(),
				LazyThreadSafetyMode.ExecutionAndPublication);
		}

		/// <inheritdoc />
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) {
			var endpointResolver = _endpoint;
			try {
				var endpoint = await endpointResolver.Value.ConfigureAwait(false);

				request.RequestUri = new UriBuilder(request.RequestUri!) {
					Host = endpoint.GetHost(),
					Port = endpoint.GetPort(),
					Scheme = _useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
				}.Uri;
				request.Headers.Add("requires-leader", _requiresLeader.ToString());
				var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

				if (!response.TrailingHeaders.TryGetValues(Constants.Exceptions.ExceptionKey, out var key) ||
				    !key.Contains(Constants.Exceptions.NotLeader) ||
				    !response.TrailingHeaders.TryGetValues(Constants.Exceptions.LeaderEndpointHost, out var hosts) ||
				    !response.TrailingHeaders.TryGetValues(Constants.Exceptions.LeaderEndpointPort, out var ports)) {
					return response;
				}

				foreach (var host in hosts) {
					foreach (var port in ports) {
						if (!int.TryParse(port, out var p)) {
							continue;
						}

						Interlocked.Exchange(ref _endpoint,
							new Lazy<Task<EndPoint>>(Task.FromResult<EndPoint>(new DnsEndPoint(host, p))));

						return response;
					}
				}

				return response;
			} catch (Exception) {
				Interlocked.CompareExchange(ref _endpoint,
					new Lazy<Task<EndPoint>>(() => _endpointDiscoverer.DiscoverAsync(cancellationToken),
						LazyThreadSafetyMode.ExecutionAndPublication), endpointResolver);

				throw;
			}
		}
	}
}
