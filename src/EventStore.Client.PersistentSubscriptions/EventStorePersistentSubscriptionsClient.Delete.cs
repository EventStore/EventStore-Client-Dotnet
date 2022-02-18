using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		/// <summary>
		/// Deletes a persistent subscription.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task DeleteAsync(string streamName, string groupName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

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
		/// <param name="groupName"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task DeleteToAllAsync(string groupName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
			await DeleteAsync(SystemStreams.AllStream, groupName, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
	}
}
