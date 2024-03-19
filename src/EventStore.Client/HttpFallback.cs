using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class HttpFallback : IDisposable {
		private readonly HttpClient _httpClient;
		private readonly JsonSerializerOptions _jsonSettings;
		private readonly UserCredentials? _defaultCredentials;
		private readonly string _addressScheme;

		internal HttpFallback (EventStoreClientSettings settings, X509Certificate2? userCertificate = null) {
			_addressScheme = settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme;
            _defaultCredentials = settings.DefaultCredentials;

            var handler = new HttpClientHandler();
            if (!settings.ConnectivitySettings.Insecure) {
	            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

	            bool configureClientCert = settings.ConnectivitySettings.UserCertificate != null
	                                    || settings.ConnectivitySettings.TlsCaFile != null
	                                    || userCertificate != null;

	            var certificate = userCertificate
	                           ?? settings.ConnectivitySettings.UserCertificate
	                           ?? settings.ConnectivitySettings.TlsCaFile;

	            if (configureClientCert) {
		            handler.ClientCertificates.Add(certificate!);
	            }

				if (!settings.ConnectivitySettings.TlsVerifyCert)
					handler.ServerCertificateCustomValidationCallback = delegate { return true; };
			}

			_httpClient = new HttpClient(handler);
			if (settings.DefaultDeadline.HasValue) {
				_httpClient.Timeout = settings.DefaultDeadline.Value;
			}
			
			_jsonSettings = new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
			};
		}

		internal async Task<T> HttpGetAsync<T>(string path, ChannelInfo channelInfo, TimeSpan? deadline,
			UserCredentials? userCredentials, Action onNotFound, CancellationToken cancellationToken) {

			var request = CreateRequest(path, HttpMethod.Get, channelInfo, userCredentials);
			
			var httpResult = await HttpSendAsync(request, onNotFound, deadline, cancellationToken).ConfigureAwait(false);
			
#if NET
			var json = await httpResult.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
			var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

			var result = JsonSerializer.Deserialize<T>(json, _jsonSettings);
			if (result == null) {
				throw new InvalidOperationException("Unable to deserialize response into object of type " + typeof(T));
			}

			return result;
		}

		internal async Task HttpPostAsync(string path, string query, ChannelInfo channelInfo, TimeSpan? deadline,
			UserCredentials? userCredentials, Action onNotFound, CancellationToken cancellationToken) {

			var request = CreateRequest(path, query, HttpMethod.Post, channelInfo, userCredentials);
			
			await HttpSendAsync(request, onNotFound, deadline, cancellationToken).ConfigureAwait(false);
		}

		private async Task<HttpResponseMessage> HttpSendAsync(HttpRequestMessage request, Action onNotFound,
			TimeSpan? deadline, CancellationToken cancellationToken) {

			if (!deadline.HasValue) {
				return await HttpSendAsync(request, onNotFound, cancellationToken).ConfigureAwait(false);				
			}
			
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(deadline.Value);
				
			return await HttpSendAsync(request, onNotFound, cts.Token).ConfigureAwait(false);
		}
		
		async Task<HttpResponseMessage> HttpSendAsync(HttpRequestMessage request, Action onNotFound,
			CancellationToken cancellationToken) {
			
			var httpResult = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (httpResult.IsSuccessStatusCode) {
				return httpResult;
			}

			if (httpResult.StatusCode == HttpStatusCode.Unauthorized) {
				throw new AccessDeniedException();
			}

			if (httpResult.StatusCode == HttpStatusCode.NotFound) {
				onNotFound();
			}

			throw new Exception($"The HTTP request failed with status code: {httpResult.StatusCode}");
		}

		private HttpRequestMessage CreateRequest(string path, HttpMethod method, ChannelInfo channelInfo,
			UserCredentials? credentials) => CreateRequest(path, query: "", method, channelInfo, credentials);

		private HttpRequestMessage CreateRequest(string path, string query, HttpMethod method, ChannelInfo channelInfo,
			UserCredentials? credentials) {
			
			var uriBuilder = new UriBuilder($"{_addressScheme}://{channelInfo.Channel.Target}") {
				Path = path,
				Query = query
			};

			var httpRequest = new HttpRequestMessage(method, uriBuilder.Uri);
			httpRequest.Headers.Add("accept", "application/json");
			credentials ??= _defaultCredentials;
			if (credentials != null) {
				httpRequest.Headers.Add(Constants.Headers.Authorization, credentials.ToString());
			}
			
			return httpRequest;
		}

		public void Dispose() {
			_httpClient.Dispose();
		}
	}
}
