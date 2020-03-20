using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public static class EventStoreUserManagerClientExtensions {
		public static Task<UserDetails> GetCurrentUserAsync(this EventStoreUserManagementClient users,
			UserCredentials userCredentials, CancellationToken cancellationToken = default)
			=> users.GetUserAsync(userCredentials.Username, userCredentials, cancellationToken);
	}
}
