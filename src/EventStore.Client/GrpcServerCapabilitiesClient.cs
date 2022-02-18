using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class GrpcServerCapabilitiesClient : IServerCapabilitiesClient {
		private readonly EventStoreClientSettings _settings;

		public GrpcServerCapabilitiesClient(EventStoreClientSettings settings) {
			_settings = settings;
		}

		public async Task<ServerCapabilities> GetAsync(
			CallInvoker callInvoker,
			CancellationToken cancellationToken) {

			var client = new ServerFeatures.ServerFeatures.ServerFeaturesClient(callInvoker);
			using var call = client.GetSupportedMethodsAsync(
				new(),
				EventStoreCallOptions.CreateNonStreaming(
					_settings,
					_settings.ConnectivitySettings.GossipTimeout,
					null,
					cancellationToken));

			try {
				var supportsBatchAppend = false;
				var supportsPersistentSubscriptionsToAll = false;

				var response = await call.ResponseAsync.ConfigureAwait(false);

				foreach (var supportedMethod in response.Methods) {
					switch (supportedMethod.ServiceName, supportedMethod.MethodName) {
						case ("event_store.client.streams.streams", "batchappend"):
							supportsBatchAppend = true;
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "read"):
							supportsPersistentSubscriptionsToAll = supportedMethod.Features.Contains("all");
							continue;
					}
				}

				return new(
					SupportsBatchAppend: supportsBatchAppend,
					SupportsPersistentSubscriptionsToAll: supportsPersistentSubscriptionsToAll);

			} catch (Exception ex) when (ex.GetBaseException() is RpcException rpcException &&
				rpcException.StatusCode == StatusCode.Unimplemented) {

				return new();
			}
		}
	}
}
