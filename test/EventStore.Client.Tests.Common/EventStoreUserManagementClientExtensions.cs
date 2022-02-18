using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;

#nullable enable
namespace EventStore.Client {
	internal static class EventStoreUserManagementClientExtensions {
		public static Task CreateUserWithRetry(this EventStoreUserManagementClient client, string loginName,
			string fullName, string[] groups, string password,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			Policy.Handle<NotAuthenticatedException>()
				.WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100))
				.ExecuteAsync(
					ct => client.CreateUserAsync(loginName, fullName, groups, password,
						userCredentials: userCredentials, cancellationToken: ct), cancellationToken);
	}
}
