using Polly;
using static System.TimeSpan;

namespace EventStore.Client.Tests;

public static class EventStoreClientExtensions {
    public static Task CreateUserWithRetry(
        this EventStoreUserManagementClient client, string loginName, string fullName, string[] groups, string password,
        UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
    ) =>
        Policy.Handle<NotAuthenticatedException>()
            .WaitAndRetryAsync(200, _ => FromMilliseconds(100))
            .ExecuteAsync(
                ct => client.CreateUserAsync(
                    loginName, fullName, groups, password, 
                    userCredentials: userCredentials, 
                    cancellationToken: ct
                ),
                cancellationToken
            );
    
	// // This executes `warmup` with some somewhat subtle retry logic:
	// //     execute the `warmup`.
	// //     if it succeeds we are done.
	// //     if it throws an exception, wait a short time (100ms) and try again.
	// //     if it hangs
	// //         1. cancel it after a little while (backing off),
	// //         2. trigger rediscovery
	// //         3. try again.
	// //     eventually give up retrying.
	// public static Task WarmUpWith(
	// 	this EventStoreClientBase self,
	// 	Func<CancellationToken, Task> warmup) {
	//
	// 	const string retryCountKey = "retryCount";
	// 	var rediscover = typeof(EventStoreClientBase).GetMethod(
	// 		"Rediscover",
	// 		BindingFlags.NonPublic | BindingFlags.Instance)!;
	//
	// 	return Policy.Handle<Exception>()
	// 		.WaitAndRetryAsync(
	// 			retryCount: 200,
	// 			sleepDurationProvider: (retryCount, context) => {
	// 				context[retryCountKey] = retryCount;
	// 				return TimeSpan.FromMilliseconds(100);
	// 			},
	// 			onRetry: (ex, slept, context) => { })
	// 		.WrapAsync(
	// 			Policy.TimeoutAsync(
	// 				timeoutProvider: context => {
	// 					// decide how long to allow for the call (including discovery if it is pending)
	// 					var retryCount = (int)context[retryCountKey];
	// 					var retryMs    = retryCount * 100;
	// 					retryMs = Math.Max(retryMs, 100); // wait at least
	// 					retryMs = Math.Min(retryMs, 2000); // wait at most
	// 					return TimeSpan.FromMilliseconds(retryMs);
	// 				},
	// 				onTimeoutAsync: (context, timeout, task, ex) => {
	// 					// timed out from the TimeoutPolicy, perhaps its broken. trigger rediscovery
	// 					// (if discovery is in progress it will continue, not restart)
	// 					rediscover.Invoke(self, Array.Empty<object>());
	// 					return Task.CompletedTask;
	// 				}))
	// 		.ExecuteAsync(
	// 			async (context, cancellationToken) => {
	// 				try {
	// 					await warmup(cancellationToken);
	// 				} catch (Exception ex) when (ex is not OperationCanceledException) {
	// 					// grpc throws a rpcexception when you cancel the token (which we convert into
	// 					// invalid operation) - but polly expects operationcancelledexception or it wont
	// 					// call onTimeoutAsync. so raise that here.
	// 					cancellationToken.ThrowIfCancellationRequested();
	// 					throw;
	// 				}
	// 			},
	// 			contextData: new Dictionary<string, object> { { retryCountKey, 0 } },
	// 			CancellationToken.None);
	// }
}