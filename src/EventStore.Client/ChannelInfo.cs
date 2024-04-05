#pragma warning disable 1591

using Grpc.Core;

namespace EventStore.Client;

public record ChannelInfo(
	ChannelBase Channel,
	ServerCapabilities ServerCapabilities,
	CallInvoker CallInvoker
);