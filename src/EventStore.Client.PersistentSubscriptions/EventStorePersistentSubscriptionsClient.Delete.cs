using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		/// <summary>
		/// Deletes a persistent subscription.
		/// </summary>
		[Obsolete("DeleteAsync is no longer supported. Use DeleteToStreamAsync instead.", false)]
		public Task DeleteAsync(string streamName, string groupName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
			DeleteToStreamAsync(streamName, groupName, deadline, userCredentials, cancellationToken);

		/// <summary>
		/// Deletes a persistent subscription.
		/// </summary>
		public async Task DeleteToStreamAsync(string streamName, string groupName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(userCredentials?.UserCertificate, cancellationToken).ConfigureAwait(false);

			if (streamName == SystemStreams.AllStream &&
			    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
				throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
			}

			var deleteOptions = new DeleteReq.Types.Options {
				GroupName = groupName
			};

			if (streamName == SystemStreams.AllStream) {
				deleteOptions.All = new Empty();
			} else {
				deleteOptions.StreamIdentifier = streamName;
			}

			using var call =
				new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(
						channelInfo.CallInvoker)
					.DeleteAsync(new DeleteReq {Options = deleteOptions},
						EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials,
							cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes a persistent subscription to $all.
		/// </summary>
		public async Task DeleteToAllAsync(string groupName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
			await DeleteToStreamAsync(SystemStreams.AllStream, groupName, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
	}
}
