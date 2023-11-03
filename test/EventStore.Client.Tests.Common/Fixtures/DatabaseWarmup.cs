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
    
    static DatabaseWarmup() {
		AppDomain.CurrentDomain.DomainUnload += (_, _) => Completed.Set(false);
    }

    public static async Task TryExecuteOnce(T client, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) {
	    await Semaphore.WaitAsync(cancellationToken);

	    try {
		    if (!Completed.EnsureCalledOnce()) {
			    Logger.Warning("*** Warmup started ***");
			    await TryExecute(client, action, cancellationToken);
			    Logger.Warning("*** Warmup completed ***");
		    }
		    else {
			    Logger.Information("*** Warmup skipped ***");
		    }
	    }
	    finally {
		    Semaphore.Release();
	    }
    }
    
    static Task TryExecute(EventStoreClientBase client, Func<CancellationToken, Task> action, CancellationToken cancellationToken) {
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

        return policy.ExecuteAsync(async ct => {
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
        }, cancellationToken);
    }
}