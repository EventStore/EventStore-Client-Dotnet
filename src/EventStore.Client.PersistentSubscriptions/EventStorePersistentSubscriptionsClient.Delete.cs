using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		public async Task DeleteAsync(string streamName, string groupName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.DeleteAsync(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					StreamName = streamName,
					GroupName = groupName
				}
			}, RequestMetadata.Create(userCredentials), cancellationToken: cancellationToken);
		}
	}
}
