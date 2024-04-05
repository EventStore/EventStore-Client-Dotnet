using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace EventStore.Client;

class HttpFallback : IDisposable {
	readonly string                _addressScheme;
	readonly UserCredentials?      _defaultCredentials;
	readonly HttpClient            _httpClient;
	readonly JsonSerializerOptions _jsonSettings;

	internal HttpFallback(EventStoreClientSettings settings) {
		_addressScheme      = settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme;
		_defaultCredentials = settings.DefaultCredentials;

		var handler = new HttpClientHandler();
		if (!settings.ConnectivitySettings.Insecure) {
			handler.ClientCertificateOptions = ClientCertificateOption.Manual;

			if (settings.ConnectivitySettings.TlsCaFile != null)
				handler.ClientCertificates.Add(settings.ConnectivitySettings.TlsCaFile);

			if (!settings.ConnectivitySettings.TlsVerifyCert)
				handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
		}

		_httpClient = new HttpClient(handler);
		if (settings.DefaultDeadline.HasValue) _httpClient.Timeout = settings.DefaultDeadline.Value;

		_jsonSettings = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}

	public void Dispose() {
		_httpClient.Dispose();
	}

	internal async Task<T> HttpGetAsync<T>(
		string path, ChannelInfo channelInfo, TimeSpan? deadline,
		UserCredentials? userCredentials, Action onNotFound, CancellationToken cancellationToken
	) {
		var request = CreateRequest(path, HttpMethod.Get, channelInfo, userCredentials);

		var httpResult = await HttpSendAsync(request, onNotFound, deadline, cancellationToken).ConfigureAwait(false);

#if NET
		var json = await httpResult.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
			var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

		var result = JsonSerializer.Deserialize<T>(json, _jsonSettings);
		if (result == null) throw new InvalidOperationException("Unable to deserialize response into object of type " + typeof(T));

		return result;
	}

	internal async Task HttpPostAsync(
		string path, string query, ChannelInfo channelInfo, TimeSpan? deadline,
		UserCredentials? userCredentials, Action onNotFound, CancellationToken cancellationToken
	) {
		var request = CreateRequest(path, query, HttpMethod.Post, channelInfo, userCredentials);

		await HttpSendAsync(request, onNotFound, deadline, cancellationToken).ConfigureAwait(false);
	}

	async Task<HttpResponseMessage> HttpSendAsync(
		HttpRequestMessage request, Action onNotFound,
		TimeSpan? deadline, CancellationToken cancellationToken
	) {
		if (!deadline.HasValue) return await HttpSendAsync(request, onNotFound, cancellationToken).ConfigureAwait(false);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(deadline.Value);

		return await HttpSendAsync(request, onNotFound, cts.Token).ConfigureAwait(false);
	}

	async Task<HttpResponseMessage> HttpSendAsync(
		HttpRequestMessage request, Action onNotFound,
		CancellationToken cancellationToken
	) {
		var httpResult = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (httpResult.IsSuccessStatusCode) return httpResult;

		if (httpResult.StatusCode == HttpStatusCode.Unauthorized) throw new AccessDeniedException();

		if (httpResult.StatusCode == HttpStatusCode.NotFound) onNotFound();

		throw new Exception($"The HTTP request failed with status code: {httpResult.StatusCode}");
	}

	HttpRequestMessage CreateRequest(
		string path, HttpMethod method, ChannelInfo channelInfo,
		UserCredentials? credentials
	) => CreateRequest(path, "", method, channelInfo, credentials);

	HttpRequestMessage CreateRequest(
		string path, string query, HttpMethod method, ChannelInfo channelInfo,
		UserCredentials? credentials
	) {
		var uriBuilder = new UriBuilder($"{_addressScheme}://{channelInfo.Channel.Target}") {
			Path  = path,
			Query = query
		};

		var httpRequest = new HttpRequestMessage(method, uriBuilder.Uri);
		httpRequest.Headers.Add("accept", "application/json");
		credentials ??= _defaultCredentials;
		if (credentials != null) httpRequest.Headers.Add(Constants.Headers.Authorization, credentials.ToString());

		return httpRequest;
	}
}