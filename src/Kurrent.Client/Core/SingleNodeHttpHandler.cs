using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class SingleNodeHttpHandler : DelegatingHandler {
		private readonly KurrentClientSettings _settings;

		public SingleNodeHttpHandler(KurrentClientSettings settings) {
			_settings = settings;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) {
			request.RequestUri = new UriBuilder(request.RequestUri!) {
				Scheme = _settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
			}.Uri;
			return base.SendAsync(request, cancellationToken);
		}
	}
}
