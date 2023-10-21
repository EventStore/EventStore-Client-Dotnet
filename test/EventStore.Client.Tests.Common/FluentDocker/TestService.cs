using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;

namespace EventStore.Client.Tests.FluentDocker;

// public interface ITestService : IDisposable {
//     void Start();
//     void Stop();
// }
//
// public abstract class TestService<TService, TBuilder> : ITestService where TService : IService where TBuilder : BaseBuilder<TService>  {
//     protected TService Service { get; private set; } = default!;
//
//     public void Start() {
//         try {
//             var builder = Configure();
//             Service = builder.Build();
//             Service.Start();
//         }
//         catch(Exception ex) {
//             throw new FluentDockerException($"Failed to start service {Service.Name}", ex);
//         }
//
//         try {
//             OnServiceStarted();
//         }
//         catch (Exception ex) {
//             throw new($"{nameof(OnServiceStarted)} execution error", ex);
//         }
//     }
//
//     public void Stop() {
//         try {
//             OnServiceStop();
//         }
//         catch (Exception ex) {
//             throw new($"{nameof(OnServiceStop)} execution error", ex);
//         }
//         
//         try {
//             Service.Stop();
//         }
//         catch (Exception ex) {
//             throw new FluentDockerException($"Failed to stop service {Service.Name}", ex);
//         }
//     }
//
//     public void Dispose() {
//         Stop();
//         
//         try {
//             if (Service.State != ServiceRunningState.Unknown) {
//                 Service.Dispose();    
//             }
//         }
//         catch(Exception ex) {
//             throw new FluentDockerException($"Failed to dispose of service {Service.Name}", ex);
//         }
//     }
//
//     protected abstract TBuilder Configure();
//
//     protected virtual Task OnServiceStarted() => Task.CompletedTask;
//     protected virtual Task OnServiceStop()    => Task.CompletedTask;
// }


public interface ITestService : IAsyncDisposable {
    Task Start();
    Task Stop();
}

public abstract class TestService<TService, TBuilder> : ITestService where TService : IService where TBuilder : BaseBuilder<TService> {
    protected TService Service { get; private set; } = default!;

    public async Task Start() {
        try {
            var builder = Configure();
            Service = builder.Build();
        }
        catch (Exception ex) {
            throw new FluentDockerException($"Failed to configure service {Service.Name}", ex);
        }
        
        try {
            Service.Start();
        }
        catch (Exception ex) {
            throw new FluentDockerException($"Failed to start service {Service.Name}", ex);
        }

        try {
            await OnServiceStarted();
        }
        catch (Exception ex) {
            throw new FluentDockerException($"{nameof(OnServiceStarted)} execution error", ex);
        }
    }

    public async Task Stop() {
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
            throw new FluentDockerException($"Failed to stop service {Service.Name}", ex);
        }
    }

    public ValueTask DisposeAsync() {
        try {
            if (Service.State != ServiceRunningState.Unknown) {
                Service.Dispose();
            }
        }
        catch (Exception ex) {
            throw new FluentDockerException($"Failed to dispose of service {Service.Name}", ex);
        }
        
        return ValueTask.CompletedTask;
    }

    protected abstract TBuilder Configure();
    
    protected virtual Task OnServiceStarted() => Task.CompletedTask;
    protected virtual Task OnServiceStop()    => Task.CompletedTask;
}


public abstract class TestCompositeService : TestService<ICompositeService, CompositeBuilder> { }

public abstract class TestContainerService : TestService<IContainerService, ContainerBuilder> { }

