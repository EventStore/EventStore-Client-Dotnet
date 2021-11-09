using Grpc.Core;

namespace EventStore.Client {
#pragma warning disable 1591
	public record ChannelInfo(
		ChannelBase Channel,
		ServerCapabilities ServerCapabilities,
		CallInvoker CallInvoker);
}
