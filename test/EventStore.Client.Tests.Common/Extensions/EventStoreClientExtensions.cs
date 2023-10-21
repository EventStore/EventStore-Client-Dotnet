using Polly;
using Polly.Contrib.WaitAndRetry;
using static System.TimeSpan;

namespace EventStore.Client.Tests; 

public static class EventStoreClientExtensions {
	public static Task TryExecute(this EventStoreClientBase client, Func<CancellationToken, Task> action) {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: FromMilliseconds(100), retryCount: 100, fastFirst: true);
        var retry = Policy.Handle<Exception>().WaitAndRetryAsync(delay);

        var rediscoverTimeout = Policy.TimeoutAsync(FromSeconds(30), (_, __, ___) => client.RediscoverAsync());
        var executionTimeout  = Policy.TimeoutAsync(FromSeconds(180));

        var policy = executionTimeout
            .WrapAsync(rediscoverTimeout.WrapAsync(retry));

		return policy.ExecuteAsync(ct => Execute(ct), CancellationToken.None);

		async Task Execute(CancellationToken ct) {
			try {
				await action(ct);
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				// grpc throws a rpcexception when you cancel the token (which we convert into
				// invalid operation) - but polly expects operationcancelledexception or it wont
				// call onTimeoutAsync. so raise that here.
				ct.ThrowIfCancellationRequested();
				throw;
			}
		}
	}
	
	public static Task WarmUp(this EventStoreClient client) =>
		client.TryExecute(async ct => {
			// if we can read from $users then we know that
			// 1. the users exist
			// 2. we are connected to leader if we require it
			var users = await client
				.ReadStreamAsync(
					direction: Direction.Forwards,
					streamName: "$users",
					revision: StreamPosition.Start, 
					maxCount: 1,
					userCredentials: TestCredentials.Root,
					cancellationToken: ct)
				.ToArrayAsync(ct);

			if (users.Length == 0)
				throw new ("System is not ready yet...");

			// the read from leader above is not enough to guarantee the next write goes to leader
			_ = await client.AppendToStreamAsync(
				streamName: "warmup", 
				expectedState: StreamState.Any, 
				eventData: Enumerable.Empty<EventData>(),
				userCredentials: TestCredentials.Root,
				cancellationToken: ct
			);
		});

    public static Task WarmUp(this EventStoreOperationsClient client) =>
        client.TryExecute(
            ct => client.RestartPersistentSubscriptions(
                userCredentials: TestCredentials.Root,
                cancellationToken: ct
            )
        );

    public static Task WarmUp(this EventStorePersistentSubscriptionsClient client) =>
        client.TryExecute(
            ct => {
                var id = Guid.NewGuid();
                return client.CreateToStreamAsync(
                    streamName: $"warmup-stream-{id}",
                    groupName: $"warmup-group-{id}",
                    settings: new(),
                    userCredentials: TestCredentials.Root,
                    cancellationToken: ct
                );
            }
        );

    public static Task WarmUp(this EventStoreProjectionManagementClient client) =>
        client.TryExecute(
            async ct => await client
                .ListAllAsync(
                    userCredentials: TestCredentials.Root, 
                    cancellationToken: ct
                )
                .ToArrayAsync(ct)
        );

    public static Task WarmUp(this EventStoreUserManagementClient client) =>
        client.TryExecute(
            async ct => await client
                .ListAllAsync(
                    userCredentials: TestCredentials.Root, 
                    cancellationToken: ct
                )
                .ToArrayAsync(ct)
        );

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
