using System.Reflection;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Compose;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Impl;
using Polly;
using Polly.Contrib.WaitAndRetry;
using static System.TimeSpan;

namespace EventStore.Client.Tests.FluentDocker;

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
}

[PublicAPI]
public static class FluentDockerContainerBuilderExtensions {
    public static ContainerBuilder WithEnvironment(this ContainerBuilder builder, Dictionary<string, string?> environment) =>
        builder.WithEnvironment(environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray());

    public static ContainerBuilder WaitUntilReady(this ContainerBuilder builder, IEnumerable<TimeSpan> sleepDurations, Action<IContainerService> action) =>
        builder.Wait("", (service, _) => {
            var result = Policy
                .Handle<Exception>()
                .WaitAndRetry(sleepDurations)
                .ExecuteAndCapture(() => action(service));

            if (result.Outcome == OutcomeType.Successful)
                return 0;

            throw result.FinalException is FluentDockerException
                ? result.FinalException
                : new FluentDockerException($"Service {service.Name} not ready: {result.FinalException.Message}");
        });

    public static ContainerBuilder WaitUntilReadyWithConstantBackoff(
        this ContainerBuilder builder, TimeSpan delay, int retryCount, Action<IContainerService> action
    ) => builder.WaitUntilReady(Backoff.ConstantBackoff(delay, retryCount), action);

    public static ContainerBuilder WaitUntilReadyWithExponentialBackoff(
        this ContainerBuilder builder, TimeSpan delay, int retryCount, Action<IContainerService> action
    ) => builder.WaitUntilReady(Backoff.ExponentialBackoff(delay, retryCount), action);

    public static ContainerBuilder WaitUntilReadyWithConstantBackoff(
        this ContainerBuilder builder, int delayMs, int retryCount, Action<IContainerService> action
    ) => builder.WaitUntilReadyWithConstantBackoff(FromMilliseconds(delayMs), retryCount, action);

    public static ContainerBuilder WaitUntilReadyWithExponentialBackoff(
        this ContainerBuilder builder, int delayMs, int retryCount, Action<IContainerService> action
    ) => builder.WaitUntilReadyWithExponentialBackoff(FromMilliseconds(delayMs), retryCount, action);

    public static ContainerBuilder WaitUntilReadyAsync(this ContainerBuilder builder, IEnumerable<TimeSpan> sleepDurations, Func<IContainerService, ValueTask> action) =>
        builder.WaitUntilReady(sleepDurations, service => {
            var valueTask = action(service);
            if (!valueTask.IsCompletedSuccessfully)
                valueTask.AsTask().GetAwaiter().GetResult();
        });

    public static ContainerBuilder WaitUntilReadyWithConstantBackoffAsync(
        this ContainerBuilder builder, TimeSpan delay, int retryCount, Func<IContainerService, ValueTask> action
    ) => builder.WaitUntilReadyAsync(Backoff.ConstantBackoff(delay, retryCount, fastFirst: true), action);

    public static ContainerBuilder WaitUntilReadyWithExponentialBackoffAsync(
        this ContainerBuilder builder, TimeSpan delay, int retryCount, Func<IContainerService, ValueTask> action
    ) => builder.WaitUntilReadyAsync(Backoff.ExponentialBackoff(delay, retryCount, fastFirst: true), action);

    public static ContainerBuilder WaitUntilReadyWithConstantBackoffAsync(
        this ContainerBuilder builder, int delayMs, int retryCount, Func<IContainerService, ValueTask> action
    ) => builder.WaitUntilReadyWithConstantBackoffAsync(FromMilliseconds(delayMs), retryCount, action);

    public static ContainerBuilder WaitUntilReadyWithExponentialBackoffAsync(
        this ContainerBuilder builder, int delayMs, int retryCount, Func<IContainerService, ValueTask> action
    ) => builder.WaitUntilReadyWithExponentialBackoffAsync(FromMilliseconds(delayMs), retryCount, action);
}