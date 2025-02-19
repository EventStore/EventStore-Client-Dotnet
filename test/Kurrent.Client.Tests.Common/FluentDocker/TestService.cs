using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using static Serilog.Core.Constants;

namespace Kurrent.Client.Tests.FluentDocker;

public interface ITestService : IAsyncDisposable {
	Task Start();
	Task Stop();
	Task Restart(TimeSpan delay);

    Task Restart();

    void ReportStatus();
}

public abstract class TestService<TService, TBuilder> : ITestService where TService : IService where TBuilder : BaseBuilder<TService> {
	ILogger Logger { get; }

	public TestService() => Logger = Log.ForContext(SourceContextPropertyName, GetType().Name);

	protected TService Service { get; private set; } = default!;

	INetworkService? Network { get; set; } = null!;

    public Task Restart()
    {
      return Restart(TimeSpan.Zero);
    }

	public virtual async Task Start() {
		Logger.Information("Container service starting");

		var builder = Configure();

		Service = builder.Build();

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

	public virtual async Task Restart(TimeSpan delay) {
		try {
			try {
				Service.Stop();
				Logger.Information("Container service stopped");
			}
			catch (Exception ex) {
				throw new FluentDockerException("Failed to stop container service", ex);
			}

			await Task.Delay(delay);

			Logger.Information("Container service starting...");

			try {
				Service.Start();
			}
			catch (Exception ex) {
				throw new FluentDockerException("Failed to start container service", ex);
			}

			try {
				await OnServiceStarted();
				Logger.Information("Container service started");
			}
			catch (Exception ex) {
				throw new FluentDockerException($"{nameof(OnServiceStarted)} execution error", ex);
			}
		}
		catch (Exception ex) {
			throw new FluentDockerException("Failed to restart container service", ex);
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
	}

	public virtual ValueTask DisposeAsync() {
		try {
			Network?.Dispose();

			try {
				Service.Dispose();
			}
			catch {
				// ignored
			}
		}
		catch (Exception ex) {
			throw new FluentDockerException("Failed to dispose of container service", ex);
		}

		return default;
	}

	protected abstract TBuilder Configure();

	protected virtual Task OnServiceStarted() => Task.CompletedTask;
	protected virtual Task OnServiceStop()    => Task.CompletedTask;
}
