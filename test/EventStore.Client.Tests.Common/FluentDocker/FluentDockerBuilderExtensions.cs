using System.Reflection;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Compose;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Impl;
using static System.TimeSpan;

namespace EventStore.Client.Tests.FluentDocker;

public static class FluentDockerServiceExtensions {
    static readonly TimeSpan DefaultRetryDelay = FromMilliseconds(100);
    
    public static async Task WaitUntilNodesAreHealthy(this IContainerService service, CancellationToken cancellationToken) {
        while (true) {
            var config = service.GetConfiguration(true);
            var status = config?.State?.Health?.Status;

            if (status is HealthState.Healthy) return;
            
            if (cancellationToken.IsCancellationRequested)
                throw new FluentDockerException($"Wait for healthy expired for container {service.Id}");
            
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(DefaultRetryDelay);
        }
    }

    public static async ValueTask WaitUntilNodesAreHealthy(this IContainerService service, TimeSpan timeout) {
        using var cts = new CancellationTokenSource(timeout);
        await WaitUntilNodesAreHealthy(service, cts.Token);
    }

    public static async Task WaitUntilNodesAreHealthy(this ICompositeService service, IEnumerable<string> services, CancellationToken cancellationToken) {
        var nodes = service.Containers.Where(x => services.Contains(x.Name));
        await Parallel.ForEachAsync(nodes, cancellationToken, async (node, ct) => await node.WaitUntilNodesAreHealthy(ct));
    }
    
    public static async Task WaitUntilNodesAreHealthy(this ICompositeService service, string serviceNamePrefix, CancellationToken cancellationToken) {
        var nodes = service.Containers.Where(x => x.Name.StartsWith(serviceNamePrefix));
        await Parallel.ForEachAsync(nodes, cancellationToken, async (node, ct) => await node.WaitUntilNodesAreHealthy(ct));
    }

    public static async Task WaitUntilNodesAreHealthy(this ICompositeService service, string serviceNamePrefix, TimeSpan timeout) {
        using var cts = new CancellationTokenSource(timeout);
        await WaitUntilNodesAreHealthy(service, serviceNamePrefix, cts.Token);
    }
}


public static class FluentDockerBuilderExtensions {
    public static CompositeBuilder OverrideConfiguration(this CompositeBuilder compositeBuilder, Action<DockerComposeConfig> configure) {
        configure(GetInternalConfig(compositeBuilder));
        return compositeBuilder;

        static DockerComposeConfig GetInternalConfig(CompositeBuilder compositeBuilder) =>
            (DockerComposeConfig)typeof(CompositeBuilder)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(compositeBuilder)!;
    }

    public static DockerComposeConfig Configuration(this ICompositeService service) =>
        (DockerComposeConfig)typeof(DockerComposeCompositeService)
            .GetProperty("Config", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service)!;
    
    // public static ICompositeService OverrideConfiguration(this ICompositeService service, Action<DockerComposeConfig> configure) {
    //     configure(GetInternalConfig(service));
    //     return service;
    //
    //     static DockerComposeConfig GetInternalConfig(ICompositeService svc) =>
    //         (DockerComposeConfig)typeof(DockerComposeCompositeService)
    //             .GetProperty("Config", BindingFlags.NonPublic | BindingFlags.Instance)!
    //             .GetValue(svc)!;
    // }
    //
    // public static ICompositeService KeepAllContainersRunning(this ICompositeService service) {
    //     return OverrideConfiguration(
    //         service, config => {
    //             config.StopOnDispose  = false;
    //             config.KeepContainers = true;
    //         }
    //     );
    // }

    // public static CompositeBuilder WaitForHealthy(this CompositeBuilder builder, TimeSpan timeout, params string[] services) {
    //     var timeoutMs = (long)timeout.TotalMilliseconds;
    //
    //     foreach (var service in services)
    //         builder.WaitForHealthy(timeoutMs, service);
    //
    //     return builder;
    // }
    //
    // static CompositeBuilder WaitForHealthy(this CompositeBuilder builder, long timeoutMs, string service) {
    //     return builder.Wait(
    //         service, (svc, _) => {
    //             svc.WaitForHealthy(timeoutMs);
    //             return 0;
    //         }
    //     );
    // }
}