#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;

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
