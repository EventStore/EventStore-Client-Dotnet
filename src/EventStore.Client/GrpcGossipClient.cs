using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class GrpcGossipClient : IGossipClient, IDisposable {
		private readonly EventStoreClientSettings _settings;
		private readonly ConcurrentDictionary<EndPoint, ChannelBase> _channels;
		private int _disposed;

		public GrpcGossipClient(EventStoreClientSettings settings) {
			_settings = settings;
			_channels = new ConcurrentDictionary<EndPoint, ChannelBase>();
		}

		public async ValueTask<ClusterMessages.ClusterInfo> GetAsync(EndPoint endPoint,
			CancellationToken cancellationToken = default) {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}

			var channel = _channels.GetOrAdd(endPoint, endpoint => ChannelFactory.CreateChannel(_settings, endpoint));

			var client = new Gossip.Gossip.GossipClient(channel);

			var result = await client.ReadAsync(new Empty(), cancellationToken: cancellationToken);

			return new ClusterMessages.ClusterInfo {
				Members = result.Members.Select(x =>
					new ClusterMessages.MemberInfo {
						InstanceId = Uuid.FromDto(x.InstanceId).ToGuid(),
						State = (ClusterMessages.VNodeState)x.State,
						IsAlive = x.IsAlive,
						EndPoint = new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port)
					}).ToArray()
			};
		}

		public void Dispose() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			foreach (var channel in _channels.Values) {
				// ReSharper disable once SuspiciousTypeConversion.Global
				if (channel is IDisposable disposable) {
					disposable.Dispose();
				} else {
					channel.ShutdownAsync();
				}
			}
		}
	}
}
