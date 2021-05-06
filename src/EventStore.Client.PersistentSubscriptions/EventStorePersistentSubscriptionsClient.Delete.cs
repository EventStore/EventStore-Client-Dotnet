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
			var deleteOptions = new DeleteReq.Types.Options {
				GroupName = groupName
			};

			if (streamName == SystemStreams.AllStream) {
				deleteOptions.All = new Empty();
			} else {
				deleteOptions.StreamIdentifier = streamName;
			}

			await new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).DeleteAsync(new DeleteReq {
				Options = deleteOptions
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
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
