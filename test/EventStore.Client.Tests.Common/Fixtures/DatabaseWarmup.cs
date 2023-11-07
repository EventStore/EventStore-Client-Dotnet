// ReSharper disable StaticMemberInGenericType

using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using static System.TimeSpan;
using static Serilog.Core.Constants;

namespace EventStore.Client.Tests;

static class DatabaseWarmup<T> where T : EventStoreClientBase {
	static readonly InterlockedBoolean Completed = new InterlockedBoolean();
	static readonly SemaphoreSlim      Semaphore = new(1, 1);
	static readonly ILogger            Logger    = Log.ForContext(SourceContextPropertyName, typeof(T).Name);

	static readonly TimeSpan ExecutionTimeout  = FromSeconds(90);
	static readonly TimeSpan RediscoverTimeout = FromSeconds(30);

	static readonly IEnumerable<TimeSpan> DefaultBackoffDelay = Backoff.DecorrelatedJitterBackoffV2(
		medianFirstRetryDelay: FromMilliseconds(100),
		retryCount: 100,
		fastFirst: false
	);

	public static async Task TryExecuteOnce(T client, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) {
		await TryExecuteOld(client, action, cancellationToken);
		
		// await Semaphore.WaitAsync(cancellationToken);
		//
		// try {
		// 	if (!Completed.EnsureCalledOnce()) {
		// 		Logger.Warning("*** Warmup started ***");
		// 		await TryExecute(client, action, cancellationToken);
		// 		Logger.Warning("*** Warmup completed ***");
		// 	}
		// 	else {
		// 		Logger.Information("*** Warmup skipped ***");
		// 	}
		// }
		// finally {
		// 	Semaphore.Release();
		// }
	}

	static async Task TryExecute(EventStoreClientBase client, Func<CancellationToken, Task> action, CancellationToken cancellationToken) {
		var retry = Policy
			.Handle<Exception>()
			.WaitAndRetryAsync(DefaultBackoffDelay);

		var rediscover = Policy.TimeoutAsync(
			RediscoverTimeout,
			async (_, _, _) => {
				Logger.Warning("*** Warmup triggering rediscovery ***");
				try {
					await client.RediscoverAsync();
				}
				catch {
					// ignored
				}
			}
		);

		var policy = Policy
			.TimeoutAsync(ExecutionTimeout)
			.WrapAsync(rediscover.WrapAsync(retry));

		await policy.ExecuteAsync(
			async ct => {
				try {
					await action(ct).ConfigureAwait(false);
				}
				catch (Exception ex) when (ex is not OperationCanceledException) {
					// grpc throws a rpcexception when you cancel the token (which we convert into
					// invalid operation) - but polly expects operationcancelledexception or it wont
					// call onTimeoutAsync. so raise that here.
					ct.ThrowIfCancellationRequested();
					throw;
				}
			},
			cancellationToken
		);
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
	public static Task TryExecuteOld(EventStoreClientBase client, Func<CancellationToken, Task> action, CancellationToken cancellationToken) {
		const string retryCountKey = "retryCount";

		return Policy.Handle<Exception>()
			.WaitAndRetryAsync(
				retryCount: 200,
				sleepDurationProvider: (retryCount, context) => {
					context[retryCountKey] = retryCount;
					return FromMilliseconds(100);
				},
				onRetry: (ex, slept, context) => { }
			)
			.WrapAsync(
				Policy.TimeoutAsync(
					timeoutProvider: context => {
						// decide how long to allow for the call (including discovery if it is pending)
						var retryCount = (int)context[retryCountKey];
						var retryMs    = retryCount * 100;
						retryMs = Math.Max(retryMs, 100);  // wait at least
						retryMs = Math.Min(retryMs, 2000); // wait at most
						return FromMilliseconds(retryMs);
					},
					onTimeoutAsync: async (context, timeout, task, ex) => {
						// timed out from the TimeoutPolicy, perhaps its broken. trigger rediscovery
						// (if discovery is in progress it will continue, not restart)
						await client.RediscoverAsync();
					}
				)
			)
			.ExecuteAsync(
				async (_, ct) => {
					try {
						await action(ct);
					}
					catch (Exception ex) when (ex is not OperationCanceledException) {
						// grpc throws a rpcexception when you cancel the token (which we convert into
						// invalid operation) - but polly expects operationcancelledexception or it wont
						// call onTimeoutAsync. so raise that here.
						ct.ThrowIfCancellationRequested();
						throw;
					}
				},
				contextData: new Dictionary<string, object> { { retryCountKey, 0 } },
				cancellationToken
			);
	}
}