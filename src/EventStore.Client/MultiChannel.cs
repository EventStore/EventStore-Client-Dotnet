using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal class MultiChannel : IDisposable {
		private readonly EventStoreClientSettings _settings;
		private readonly IEndpointDiscoverer _endpointDiscoverer;
		private readonly ConcurrentDictionary<EndPoint, ChannelBase> _channels;

		private EndPoint? _current;

		private int _disposed;

		public MultiChannel(EventStoreClientSettings settings) {
			_settings = settings;
			_endpointDiscoverer = settings.ConnectivitySettings.IsSingleNode
				? (IEndpointDiscoverer)new SingleNodeEndpointDiscoverer(settings.ConnectivitySettings.Address)
				: new GossipBasedEndpointDiscoverer(settings.ConnectivitySettings, new GrpcGossipClient(settings));
			_channels = new ConcurrentDictionary<EndPoint, ChannelBase>();
		}

		public void SetEndPoint(EndPoint value) => _current = value;

		public async Task<ChannelBase> GetCurrentChannel(CancellationToken cancellationToken = default) {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}

			var current = _current ??= await _endpointDiscoverer.DiscoverAsync(cancellationToken).ConfigureAwait(false);
			return _channels.GetOrAdd(current, ChannelFactory.CreateChannel(_settings, current));
		}



		public void Dispose() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			foreach (var channel in _channels.Values) {
				if (channel is IDisposable disposable) {
					disposable.Dispose();
				} else {
					channel.ShutdownAsync();
				}
			}
		}
	}
}
