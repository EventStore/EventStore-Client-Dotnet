using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	partial class KurrentPersistentSubscriptionsClient {
		/// <summary>
		/// Restarts the persistent subscriptions subsystem.
		/// </summary>
		public async Task RestartSubsystemAsync(TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
				await new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(channelInfo.CallInvoker)
					.RestartSubsystemAsync(new Empty(), KurrentCallOptions
						.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken))
					.ConfigureAwait(false);
				return;
			}

			await HttpPost(
				path: "/subscriptions/restart",
				query: "",
				onNotFound: () =>
					throw new Exception("Unexpected exception while restarting the persistent subscription subsystem."),
				channelInfo, deadline, userCredentials, cancellationToken)
			.ConfigureAwait(false);
		}
	}
}
