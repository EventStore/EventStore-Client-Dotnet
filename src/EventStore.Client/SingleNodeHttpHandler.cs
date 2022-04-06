using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class SingleNodeHttpHandler : DelegatingHandler {
		private readonly EventStoreClientSettings _settings;

		public SingleNodeHttpHandler(EventStoreClientSettings settings) {
			_settings = settings;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) {
			request.RequestUri = new UriBuilder(request.RequestUri!) {
				Scheme = _settings.ConnectivitySettings.Address.Scheme
			}.Uri;
			return base.SendAsync(request, cancellationToken);
		}
	}
}
