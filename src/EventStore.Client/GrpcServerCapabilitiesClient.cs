using Grpc.Core;

namespace EventStore.Client;

class GrpcServerCapabilitiesClient(EventStoreClientSettings settings) : IServerCapabilitiesClient {
	public async Task<ServerCapabilities> GetAsync(
		CallInvoker callInvoker,
		CancellationToken cancellationToken
	) {
		var client = new ServerFeatures.ServerFeatures.ServerFeaturesClient(callInvoker);
		using var call = client.GetSupportedMethodsAsync(
			new Empty(),
			EventStoreCallOptions.CreateNonStreaming(
				settings,
				settings.ConnectivitySettings.GossipTimeout,
				null,
				cancellationToken
			)
		);

		try {
			var supportsBatchAppend                             = false;
			var supportsPersistentSubscriptionsToAll            = false;
			var supportsPersistentSubscriptionsGetInfo          = false;
			var supportsPersistentSubscriptionsRestartSubsystem = false;
			var supportsPersistentSubscriptionsReplayParked     = false;
			var supportsPersistentSubscriptionsList             = false;

			var response = await call.ResponseAsync.ConfigureAwait(false);

			foreach (var supportedMethod in response.Methods)
				switch (supportedMethod.ServiceName, supportedMethod.MethodName) {
					case ("event_store.client.streams.streams", "batchappend"):
						supportsBatchAppend = true;
						continue;

					case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "read"):
						supportsPersistentSubscriptionsToAll = supportedMethod.Features.Contains("all");
						continue;

					case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "getinfo"):
						supportsPersistentSubscriptionsGetInfo = true;
						continue;

					case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "restartsubsystem"):
						supportsPersistentSubscriptionsRestartSubsystem = true;
						continue;

					case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "replayparked"):
						supportsPersistentSubscriptionsReplayParked = true;
						continue;

					case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "list"):
						supportsPersistentSubscriptionsList = true;
						continue;
				}

			return new ServerCapabilities(
				supportsBatchAppend,
				supportsPersistentSubscriptionsToAll,
				supportsPersistentSubscriptionsGetInfo,
				supportsPersistentSubscriptionsRestartSubsystem,
				supportsPersistentSubscriptionsReplayParked,
				supportsPersistentSubscriptionsList
			);
		}
		catch (Exception ex) when (ex.GetBaseException() is RpcException { StatusCode: StatusCode.Unimplemented }) {
			return new ServerCapabilities();
		}
	}
}