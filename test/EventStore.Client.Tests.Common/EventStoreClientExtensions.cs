using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace EventStore.Client {
	public static class EventStoreClientExtensions {
		public static async Task WarmUpAsync(this EventStoreClient self) {
			await self.WarmUpWith(async cancellationToken => {
				// if we can read from $users then we know that 1. the users exist
				// and 2. we are connected to leader if we require it
				var users = await self
					.ReadStreamAsync(
						Direction.Forwards,
						"$users",
						StreamPosition.Start,
						userCredentials: TestCredentials.Root,
						cancellationToken: cancellationToken)
					.ToArrayAsync(cancellationToken);

				if (users.Length == 0)
					throw new Exception("no users yet");

				// the read from leader above is not enough to guarantee the next write goes to leader
				await self.AppendToStreamAsync($"warmup", StreamState.Any, Enumerable.Empty<EventData>(),
					cancellationToken: cancellationToken);
			});
		}

		public static async Task WarmUpAsync(this EventStoreUserManagementClient self) {
			await self.WarmUpWith(async cancellationToken => {
				await self.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: cancellationToken)
					.ToArrayAsync(cancellationToken);
			});
		}

		// This executes `warmup` with some somewhat subtle retry logic:
		//     execute the `warmup`.
		//     if it succeeds we are done.
		//     if it throws an exception, wait a short time (100ms) and try again.
		//     if it hangs
		//         1. cancel it after a little while (backing off),
		//         2. trigger rediscovery
		//         3. try again.
		//     eventually give up retrying.
		public static Task WarmUpWith(
			this EventStoreClientBase self,
			Func<CancellationToken, Task> warmup) {

			const string retryCountKey = "retryCount";
			var rediscover = typeof(EventStoreClientBase).GetMethod(
				"Rediscover",
				BindingFlags.NonPublic | BindingFlags.Instance)!;

			return Policy.Handle<Exception>()
				.WaitAndRetryAsync(
					retryCount: 200,
					sleepDurationProvider: (retryCount, context) => {
						context[retryCountKey] = retryCount;
						return TimeSpan.FromMilliseconds(100);
					},
					onRetry: (ex, slept, context) => { })
				.WrapAsync(
					Policy.TimeoutAsync(
						timeoutProvider: context => {
							// decide how long to allow for the call (including discovery if it is pending)
							var retryCount = (int)context[retryCountKey];
							var retryMs = retryCount * 100;
							retryMs = Math.Max(retryMs, 100); // wait at least
							retryMs = Math.Min(retryMs, 2000); // wait at most
							return TimeSpan.FromMilliseconds(retryMs);
						},
						onTimeoutAsync: (context, timeout, task, ex) => {
							// timed out from the TimeoutPolicy, perhaps its broken. trigger rediscovery
							// (if discovery is in progress it will continue, not restart)
							rediscover.Invoke(self, Array.Empty<object>());
							return Task.CompletedTask;
						}))
				.ExecuteAsync(
					async (context, cancellationToken) => {
						try {
							await warmup(cancellationToken);
						} catch (Exception ex) when (ex is not OperationCanceledException) {
							// grpc throws a rpcexception when you cancel the token (which we convert into
							// invalid operation) - but polly expects operationcancelledexception or it wont
							// call onTimeoutAsync. so raise that here.
							cancellationToken.ThrowIfCancellationRequested();
							throw;
						}
					},
					contextData: new Dictionary<string, object> { { retryCountKey, 0 } },
					CancellationToken.None);
		}
	}
}
