using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class GrpcGossipClient : IGossipClient {
		private readonly EventStoreClientSettings _settings;

		public GrpcGossipClient(EventStoreClientSettings settings) {
			_settings = settings;
		}

		public async ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel, CancellationToken ct) {
			var client = new Gossip.Gossip.GossipClient(channel);
			using var call = client.ReadAsync(
				new Empty(),
				EventStoreCallOptions.CreateNonStreaming(_settings, ct));
			var result = await call.ResponseAsync.ConfigureAwait(false);

			return new(result.Members.Select(x =>
				new ClusterMessages.MemberInfo(
					Uuid.FromDto(x.InstanceId),
					(ClusterMessages.VNodeState)x.State,
					x.IsAlive,
					new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port))).ToArray());
		}
	}
}
