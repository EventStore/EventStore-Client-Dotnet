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
				.WaitAndRetryAsync(10, count => TimeSpan.FromMilliseconds(count * 20))
				.ExecuteAsync(ct => client.CreateUserAsync(loginName, fullName, groups, password, userCredentials, ct),
					cancellationToken);
	}
}
