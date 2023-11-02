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
    
    static DatabaseWarmup() {
		AppDomain.CurrentDomain.DomainUnload += (_, _) => Completed.Set(false);
	}
    
    public static async Task TryExecuteOnce(T client, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) {
	    await Semaphore.WaitAsync(cancellationToken);

	    try {
		    // if (!Completed.EnsureCalledOnce()) {
			    Logger.Warning("*** Warmup started ***");
			    await TryExecute(client, action, cancellationToken);
			    Logger.Warning("*** Warmup completed ***");
		    // }
		    // else {
			   //  Logger.Information("*** Warmup skipped ***");
		    // }
	    }
	    catch (Exception ex) {
		    if (Application.DebuggerIsAttached) {
			    Logger.Warning(ex, "*** Warmup failed ***");    
		    }
		    else {
			    Logger.Warning("*** Warmup failed ***");
		    }
	    }
	    finally {
		    Semaphore.Release();
	    }
    }

    static Task TryExecute(EventStoreClientBase client, Func<CancellationToken, Task> action, CancellationToken cancellationToken) {
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: FromMilliseconds(500),
            retryCount: 1000,
            fastFirst: false
        );
        
        var retry = Policy.Handle<Exception>().WaitAndRetryAsync(delay);

        var rediscoverTimeout = Policy.TimeoutAsync(
            FromSeconds(30),
            async (_, _, _) => {
                Logger.Warning("*** Triggering rediscovery ***");
                await client.RediscoverAsync();
            }
        );
        
        var executionTimeout = Policy.TimeoutAsync(FromSeconds(180));

        var policy = executionTimeout
            .WrapAsync(rediscoverTimeout.WrapAsync(retry));

        return policy.ExecuteAsync(async ct => {
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
        }, cancellationToken);
    }
}