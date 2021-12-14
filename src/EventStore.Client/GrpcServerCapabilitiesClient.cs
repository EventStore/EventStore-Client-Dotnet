using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal class GrpcServerCapabilitiesClient : IServerCapabilitiesClient {
		private readonly EventStoreClientSettings _settings;

		public GrpcServerCapabilitiesClient(EventStoreClientSettings settings) {
			_settings = settings;
		}

		public async ValueTask<ServerCapabilitiesInfo> GetAsync(ChannelBase channel,
			CancellationToken cancellationToken = default) {
			var client = new ServerFeatures.ServerFeatures.ServerFeaturesClient(channel);
			using var call = client.GetSupportedMethodsAsync(new(), EventStoreCallOptions.Create(
				_settings, _settings.OperationOptions, null, cancellationToken));
			var response = await call.ResponseAsync.ConfigureAwait(false);
			bool
				supportsBatchAppend = false,
				supportsPersistentSubscriptionsToAll = false;

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

			return new(supportsBatchAppend, supportsPersistentSubscriptionsToAll);
		}
	}
}
