#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Net;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;

namespace EventStore.Client.Tests.FluentDocker;

public static class FluentDockerServiceExtensions {
	static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(100);

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
#if NET
		await Parallel.ForEachAsync(nodes, cancellationToken, async (node, ct) => await node.WaitUntilNodesAreHealthy(ct));
#else
		Parallel.ForEach(
			nodes,
			node => { node.WaitUntilNodesAreHealthy(cancellationToken).GetAwaiter().GetResult(); }
		);
#endif
	}

	public static async Task WaitUntilNodesAreHealthy(this ICompositeService service, string serviceNamePrefix, CancellationToken cancellationToken) {
		var nodes = service.Containers.Where(x => x.Name.StartsWith(serviceNamePrefix));

#if NET
		await Parallel.ForEachAsync(nodes, cancellationToken, async (node, ct) => await node.WaitUntilNodesAreHealthy(ct));
#else
		Parallel.ForEach(
			nodes,
			node => { node.WaitUntilNodesAreHealthy(cancellationToken).GetAwaiter().GetResult(); }
		);
#endif
	}

	public static async Task WaitUntilNodesAreHealthy(this ICompositeService service, string serviceNamePrefix, TimeSpan timeout) {
		using var cts = new CancellationTokenSource(timeout);

		await WaitUntilNodesAreHealthy(service, serviceNamePrefix, cts.Token);
	}
}

public static class FluentDockerContainerServiceExtensions {
    // IPAddress.Any defaults to IPAddress.Loopback
    public static IPEndPoint GetPublicEndpoint(this IContainerService service, string portAndProtocol) {
        var endpoint = service.ToHostExposedEndpoint(portAndProtocol);
        return endpoint.Address.Equals(IPAddress.Any) ? new IPEndPoint(IPAddress.Loopback, endpoint.Port) : endpoint;
    }

    public static IPEndPoint GetPublicEndpoint(this IContainerService service, int port) =>
        service.GetPublicEndpoint($"{port}/tcp");

    public static ContainerBuilder WithPublicEndpointResolver(this ContainerBuilder builder) =>
        builder.UseCustomResolver((endpoints, portAndProtocol, dockerUri) => {
            var endpoint = endpoints.ToHostPort(portAndProtocol, dockerUri);
            return endpoint.Address.Equals(IPAddress.Any) ? new IPEndPoint(IPAddress.Loopback, endpoint.Port) : endpoint;
        });

    public static CommandResponse<IList<string>> ExecuteCommand(this IContainerService service, string command) {
        var config = service.GetConfiguration();
        return service.DockerHost.Execute(config.Id, command, service.Certificates);
    }
}