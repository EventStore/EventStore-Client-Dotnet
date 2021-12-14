using System.Net;
using Grpc.Core;

namespace EventStore.Client {
#pragma warning disable 1591
	public record ChannelInfo(ChannelBase Channel, EndPoint EndPoint);
#pragma warning restore 1591
}
