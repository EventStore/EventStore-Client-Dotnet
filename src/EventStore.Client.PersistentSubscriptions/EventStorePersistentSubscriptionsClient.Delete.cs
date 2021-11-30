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
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task DeleteAsync(string streamName, string groupName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var (channel, serverCapabilities) = await GetCurrentChannelInfo().ConfigureAwait(false);

			if (streamName == SystemStreams.AllStream &&
			    !serverCapabilities.SupportsPersistentSubscriptionsToAll) {
				throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
			}

			var callInvoker = CreateCallInvoker(channel);
			var deleteOptions = new DeleteReq.Types.Options {
				GroupName = groupName
			};

			if (streamName == SystemStreams.AllStream) {
				deleteOptions.All = new Empty();
			} else {
				deleteOptions.StreamIdentifier = streamName;
			}

			await new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker)
				.DeleteAsync(new DeleteReq {Options = deleteOptions},
					EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials,
						cancellationToken));
		}

		/// <summary>
		/// Deletes a persistent subscription to $all.
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task DeleteToAllAsync(string groupName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			await DeleteAsync(
					streamName: SystemStreams.AllStream,
					groupName: groupName,
					userCredentials: userCredentials,
					cancellationToken: cancellationToken)
				.ConfigureAwait(false);
	}
}
