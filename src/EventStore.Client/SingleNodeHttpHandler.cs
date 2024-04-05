using System.Net.Http;

namespace EventStore.Client;

class SingleNodeHttpHandler(EventStoreClientSettings settings) : DelegatingHandler {
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
		request.RequestUri = new UriBuilder(request.RequestUri!) {
			Scheme = settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
		}.Uri;

		return base.SendAsync(request, cancellationToken);
	}
}