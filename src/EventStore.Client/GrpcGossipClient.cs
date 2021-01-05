using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	internal class GrpcGossipClient : IGossipClient, IDisposable {
		private readonly EventStoreClientSettings _settings;
		private readonly ConcurrentDictionary<EndPoint, Gossip.Gossip.GossipClient> _clients;
		private int _disposed;

		public GrpcGossipClient(EventStoreClientSettings settings) {
			_settings = settings;
			_clients = new ConcurrentDictionary<EndPoint, Gossip.Gossip.GossipClient>();
		}

		public async ValueTask<ClusterMessages.ClusterInfo> GetAsync(EndPoint endPoint,
			CancellationToken cancellationToken = default) {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}

			var client = _clients.GetOrAdd(endPoint,
				endpoint => new Gossip.Gossip.GossipClient(ChannelFactory.CreateChannel(_settings, endpoint)));

			var result = await client.ReadAsync(new Empty(), cancellationToken: cancellationToken);

			return new ClusterMessages.ClusterInfo {
				Members = Array.ConvertAll(result.Members.ToArray(), x =>
					new ClusterMessages.MemberInfo {
						InstanceId = Uuid.FromDto(x.InstanceId).ToGuid(),
						State = (ClusterMessages.VNodeState)x.State,
						IsAlive = x.IsAlive,
						EndPoint = new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port)
					})
			};
		}

		public void Dispose() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			foreach (var client in _clients.Values) {
				// ReSharper disable once SuspiciousTypeConversion.Global
				if (client is IDisposable disposable) {
					disposable.Dispose();
				}
			}
		}
	}
}
