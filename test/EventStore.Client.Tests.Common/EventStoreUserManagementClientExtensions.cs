using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace EventStore.Client; 

public static class EventStoreUserManagementClientExtensions {
	public static Task CreateUserWithRetry(this EventStoreUserManagementClient client, string loginName, string fullName, string[] groups, string password,
		UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
		Policy.Handle<NotAuthenticatedException>().WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100)).ExecuteAsync(
			ct => client.CreateUserAsync(loginName, fullName, groups, password, userCredentials: userCredentials, cancellationToken: ct),
			cancellationToken);

	public static async Task WarmUpAsync(this EventStoreUserManagementClient self) {
		await self.WarmUpWith(async cancellationToken => {
			await self.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: cancellationToken).ToArrayAsync(cancellationToken);
		});
	}
}
