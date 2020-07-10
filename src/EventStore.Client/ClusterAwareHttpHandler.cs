using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public class ClusterAwareHttpHandler : DelegatingHandler {
		private readonly bool _useHttps;
		private readonly bool _requiresLeader;
		private readonly IEndpointDiscoverer _endpointDiscoverer;
		private Lazy<Task<EndPoint>> _endpoint;

		public static ClusterAwareHttpHandler Create(EventStoreClientSettings settings,
			HttpMessageHandler? httpMessageHandler = null) => new ClusterAwareHttpHandler(
			settings.ConnectivitySettings.GossipOverHttps,
			settings.ConnectivitySettings.NodePreference == NodePreference.Leader,
			new ClusterEndpointDiscoverer(
				settings.ConnectivitySettings.MaxDiscoverAttempts,
				settings.ConnectivitySettings.GossipSeeds,
				settings.ConnectivitySettings.GossipTimeout,
				settings.ConnectivitySettings.GossipOverHttps,
				settings.ConnectivitySettings.DiscoveryInterval,
				settings.ConnectivitySettings.NodePreference,
				httpMessageHandler)) {
			InnerHandler = httpMessageHandler
		};


		public ClusterAwareHttpHandler(bool useHttps, bool requiresLeader, IEndpointDiscoverer endpointDiscoverer) {
			_useHttps = useHttps;
			_requiresLeader = requiresLeader;
			_endpointDiscoverer = endpointDiscoverer;
			_endpoint = new Lazy<Task<EndPoint>>(endpointDiscoverer.DiscoverAsync,
				LazyThreadSafetyMode.ExecutionAndPublication);
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) {
			var endpointResolver = _endpoint;
			try {
				var endpoint = await endpointResolver.Value.ConfigureAwait(false);

				request.RequestUri = new UriBuilder(request.RequestUri) {
					Host = endpoint.GetHost(),
					Port = endpoint.GetPort(),
					Scheme = _useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
				}.Uri;
				request.Headers.Add("requires-leader", _requiresLeader.ToString());
				return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
			} catch (Exception) {
				Interlocked.CompareExchange(ref _endpoint,
					new Lazy<Task<EndPoint>>(() => _endpointDiscoverer.DiscoverAsync(),
						LazyThreadSafetyMode.ExecutionAndPublication), endpointResolver);

				throw;
			}
		}

		public void ExceptionOccurred(Exception exception) {
			if (exception is NotLeaderException ex) {
				_endpoint = new Lazy<Task<EndPoint>>(Task.FromResult(ex.LeaderEndpoint));
			}
		}
	}
}
