using Ductus.FluentDocker;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Serilog;
using static Serilog.Core.Constants;

namespace EventStore.Client.Tests.FluentDocker;

public interface ITestService : IAsyncDisposable {
    Task Start();
    Task Stop();

    void ReportStatus();
}

/// <summary>
/// This prevents multiple services from starting at the same time.
/// Required to avoid failures on creating the networks the containers are attached to. 
/// </summary>
sealed class TestServiceGatekeeper {
    static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static Task Wait() => Semaphore.WaitAsync();
    public static void Next() => Semaphore.Release();
}

public abstract class TestService<TService, TBuilder> : ITestService where TService : IService where TBuilder : BaseBuilder<TService> {
    ILogger Logger { get; }

    public TestService() => Logger = Log.ForContext(SourceContextPropertyName, GetType().Name);

    protected TService Service { get; private set; } = default!;
    
    INetworkService? Network { get; set; } = null!;
    
    public virtual async Task Start() {
        Logger.Information("Container service starting");
        
        //await TestServiceGatekeeper.Wait();
        
        try {
	        var builder = Configure();

	        Service = builder.Build();

	        // // for some reason fluent docker does not always create the network
	        // // before the service is started, so we do it manually here
	        // if (Service is IContainerService service) {
		       //  var cfg = service.GetConfiguration(true);
	        //
		       //  Network = Fd
			      //   .UseNetwork(cfg.Name)
			      //   .IsInternal()
			      //   .Build()
			      //   .Attach(service, true);
	        //
		       //  Logger.Information("Created network {Network}", Network.Name);
	        // }

	        try {
		        Service.Start();
		        Logger.Information("Container service started");
	        }
	        catch (Exception ex) {
		        throw new FluentDockerException("Failed to start container service", ex);

	        }

            try {
                await OnServiceStarted();
            }
            catch (Exception ex) {
                throw new FluentDockerException($"{nameof(OnServiceStarted)} execution error", ex);
            }
        }
        finally {
            //TestServiceGatekeeper.Next();
        }
    }

    public virtual async Task Stop() {
        try {
            await OnServiceStop();
        }
        catch (Exception ex) {
            throw new FluentDockerException($"{nameof(OnServiceStop)} execution error", ex);
        }

        try {
            Service.Stop();
        }
        catch (Exception ex) {
            throw new FluentDockerException("Failed to stop container service", ex);
        }
    }

    public void ReportStatus() {
	    if (Service is IContainerService containerService) {
		    ReportContainerStatus(containerService);
	    }

	    if (Service is ICompositeService compose) {
			foreach (var container in compose.Containers) {
				ReportContainerStatus(container);
			}
		}

	    return;

	    void ReportContainerStatus(IContainerService service) {
		    var cfg = service.GetConfiguration(true);
		    Logger.Information("Container {Name} {State} Ports: {Ports}", service.Name, service.State, cfg.Config.ExposedPorts.Keys);
	    }

	    // var docker = Fd.Hosts().Discover().FirstOrDefault(x => x.IsNative || x.Name == "default")!;
    }

    public virtual ValueTask DisposeAsync() {
        try {
            Network?.Dispose();
            
            if (Service.State != ServiceRunningState.Unknown) {
                Service.Dispose();
            }
        }
        catch (Exception ex) {
            throw new FluentDockerException("Failed to dispose of container service", ex);
        }
        
        return ValueTask.CompletedTask;
    }

    protected abstract TBuilder Configure();
    
    protected virtual Task OnServiceStarted() => Task.CompletedTask;
    protected virtual Task OnServiceStop()    => Task.CompletedTask;
}