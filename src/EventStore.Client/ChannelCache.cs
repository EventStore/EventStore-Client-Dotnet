using System.Net;
using TChannel = Grpc.Net.Client.GrpcChannel;

namespace EventStore.Client {
	// Maintains Channels keyed by DnsEndPoint so the channels can be reused.
	// Deals with the disposal difference between grpc.net and grpc.core
	// Thread safe.
	internal class ChannelCache :
		IAsyncDisposable {

		private readonly EventStoreClientSettings _settings;
		private readonly Random _random;
		private readonly Dictionary<DnsEndPoint, TChannel> _channels;
		private readonly object _lock = new();
		private bool _disposed;

		public ChannelCache(EventStoreClientSettings settings) {
			_settings = settings;
			_random = new Random(0);
			_channels = new Dictionary<DnsEndPoint, TChannel>(
				DnsEndPointEqualityComparer.Instance);
		}

		public TChannel GetChannelInfo(DnsEndPoint endPoint) {
			lock (_lock) {
				ThrowIfDisposed();

				if (!_channels.TryGetValue(endPoint, out var channel)) {
					channel = ChannelFactory.CreateChannel(
						settings: _settings, 
						endPoint: endPoint);
					_channels[endPoint] = channel;
				}

				return channel;
			}
		}

		public KeyValuePair<DnsEndPoint, TChannel>[] GetRandomOrderSnapshot() {
			lock (_lock) {
				ThrowIfDisposed();

				return _channels
					.OrderBy(_ => _random.Next())
					.ToArray();
			}
		}

		// Update the cache to contain channels for exactly these endpoints
		public void UpdateCache(IEnumerable<DnsEndPoint> endPoints) {
			lock (_lock) {
				ThrowIfDisposed();

				// remove
				var endPointsToDiscard = _channels.Keys
					.Except(endPoints, DnsEndPointEqualityComparer.Instance)
					.ToArray();

				var channelsToDispose = new List<TChannel>(endPointsToDiscard.Length);

				foreach (var endPoint in endPointsToDiscard) {
					if (!_channels.TryGetValue(endPoint, out var channel))
						continue;

					_channels.Remove(endPoint);
					channelsToDispose.Add(channel);
				}

				_ = DisposeChannelsAsync(channelsToDispose);

				// add
				foreach (var endPoint in endPoints) {
					GetChannelInfo(endPoint);
				}
			}
		}

		public void Dispose() {
			lock (_lock) {
				if (_disposed)
					return;

				_disposed = true;

				foreach (var channel in _channels.Values) {
					channel.Dispose();
				}

				_channels.Clear();
			}
		}

		public async ValueTask DisposeAsync() {
			var channelsToDispose = Array.Empty<TChannel>();

			lock (_lock) {
				if (_disposed)
					return;
				_disposed = true;

				channelsToDispose = _channels.Values.ToArray();
				_channels.Clear();
			}

			await DisposeChannelsAsync(channelsToDispose).ConfigureAwait(false);
		}

		private void ThrowIfDisposed() {
			lock (_lock) {
				if (_disposed) {
					throw new ObjectDisposedException(GetType().ToString());
				}
			}
		}

		private static async Task DisposeChannelsAsync(IEnumerable<TChannel> channels) {
			foreach (var channel in channels)
				await channel.DisposeAsync().ConfigureAwait(false);
		}

		private class DnsEndPointEqualityComparer : IEqualityComparer<DnsEndPoint> {
			public static readonly DnsEndPointEqualityComparer Instance = new();

			public bool Equals(DnsEndPoint? x, DnsEndPoint? y) {
				if (ReferenceEquals(x, y))
					return true;
				if (x is null)
					return false;
				if (y is null)
					return false;
				if (x.GetType() != y.GetType())
					return false;
				return
					string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase) &&
					x.Port == y.Port;
			}

			public int GetHashCode(DnsEndPoint obj) {
				unchecked {
					return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Host) * 397) ^
						   obj.Port;
				}
			}
		}
	}
}
