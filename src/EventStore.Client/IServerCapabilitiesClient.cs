using Grpc.Core;

namespace EventStore.Client;

interface IServerCapabilitiesClient {
	public Task<ServerCapabilities> GetAsync(CallInvoker callInvoker, CancellationToken cancellationToken);
}