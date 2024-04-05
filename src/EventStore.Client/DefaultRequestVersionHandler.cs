using System.Net.Http;

namespace EventStore.Client;

class DefaultRequestVersionHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler) {
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
		request.Version = new Version(2, 0);
		return base.SendAsync(request, cancellationToken);
	}
}