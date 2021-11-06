using System;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable
namespace EventStore.Client {
	internal record ChannelInfo(EndPoint EndPoint, ChannelBase Channel, Task<ServerCapabilities> Capabilities);
	internal class MultiChannel : IDisposable, IAsyncDisposable {
		private readonly EventStoreClientSettings _settings;
		private readonly IEndpointDiscoverer _endpointDiscoverer;
		private readonly ConcurrentDictionary<EndPoint, ChannelInfo> _channels;
		private readonly ILogger<MultiChannel> _log;

		private EndPoint? _current;
		private int _disposed;

		public MultiChannel(EventStoreClientSettings settings) {
			_settings = settings;
			_endpointDiscoverer = settings.ConnectivitySettings.IsSingleNode
				? new SingleNodeEndpointDiscoverer(settings.ConnectivitySettings.Address)
				: new GossipBasedEndpointDiscoverer(settings.ConnectivitySettings, new GrpcGossipClient(settings));
			_channels = new ConcurrentDictionary<EndPoint, ChannelInfo>();
			_log = settings.LoggerFactory?.CreateLogger<MultiChannel>() ?? new NullLogger<MultiChannel>();

			if (settings.ConnectivitySettings.KeepAliveInterval < TimeSpan.FromSeconds(10)) {
				_log.LogWarning("Specified KeepAliveInterval of {interval} is less than recommended 10_000 ms.",
					settings.ConnectivitySettings.KeepAliveInterval);
			}
		}

		public void SetEndPoint(EndPoint value) {
			ThrowIfDisposed();
			_current = value;
		}

		public void EvictChannel(EndPoint channelEndpoint) {
			ThrowIfDisposed();
			_channels.TryRemove(channelEndpoint, out _);
		}

		public async Task<ChannelInfo> GetCurrentChannel(CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			var current = _current ??= await _endpointDiscoverer.DiscoverAsync(cancellationToken).ConfigureAwait(false);
			return _channels.GetOrAdd(current, GetChannelByEndPoint);
		}

		private void ThrowIfDisposed() {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}
		}

		private ChannelInfo GetChannelByEndPoint(EndPoint key) {
			var channel = ChannelFactory.CreateChannel(_settings, key);

			var serverCapabilities = GetServerCapabilities(key);
			serverCapabilities.ContinueWith(ShutdownChannel,
				TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
			return new(key, channel, serverCapabilities);

			void ShutdownChannel(Task<ServerCapabilities> _) {
				_channels.TryRemove(key, out var info);
				info?.Channel.ShutdownAsync();
			}
		}

		private async Task<ServerCapabilities> GetServerCapabilities(EndPoint endPoint,
			CancellationToken cancellationToken = default) {
			var channel = ChannelFactory.CreateChannel(_settings, endPoint);

			try {
				var client = new ServerFeatures.ServerFeatures.ServerFeaturesClient(channel);

				var response = await client.GetSupportedMethodsAsync(new Empty(),
					EventStoreCallOptions.Create(_settings, _settings.OperationOptions, null, cancellationToken));

				bool supportsBatchAppend = false;

				foreach (var supportedMethod in response.Methods) {
					if (supportedMethod.MethodName == "batchappend" &&
					    supportedMethod.ServiceName == "event_store.client.streams.streams") {
						supportsBatchAppend = true;
					}
				}

				return new(supportsBatchAppend);
			} catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.Unimplemented) {
				return new();
			} finally {
				await channel.ShutdownAsync().ConfigureAwait(false);
			}
		}

		public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

		public async ValueTask DisposeAsync() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			foreach (var (_, channel, _) in _channels.Values) {
				await channel.ShutdownAsync().ConfigureAwait(false);
			}
		}
	}
}
