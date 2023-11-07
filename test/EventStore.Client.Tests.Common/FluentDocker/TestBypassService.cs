using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;

namespace EventStore.Client.Tests.FluentDocker;

public class TestBypassService : TestService<BypassService, BypassBuilder> {
	protected override BypassBuilder Configure() => throw new NotImplementedException();

	public override async Task Start() {
		try {
			await OnServiceStarted();
		}
		catch (Exception ex) {
			throw new FluentDockerException($"{nameof(OnServiceStarted)} execution error", ex);
		}
	}

	public override async Task Stop() {
		try {
			await OnServiceStop();
		}
		catch (Exception ex) {
			throw new FluentDockerException($"{nameof(OnServiceStop)} execution error", ex);
		}
	}

	public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class BypassService : IService {
	public string              Name  { get; } = nameof(BypassService);
	public ServiceRunningState State { get; } = ServiceRunningState.Unknown;

	public void Dispose() { }

	public void Start() { }

	public void Pause() { }

	public void Stop() { }

	public void Remove(bool force = false) { }

	public IService AddHook(ServiceRunningState state, Action<IService> hook, string? uniqueName = null) => this;

	public IService RemoveHook(string uniqueName) => this;

	public event ServiceDelegates.StateChange? StateChange;

	void OnStateChange(StateChangeEventArgs evt) => StateChange?.Invoke(this, evt);
}

public sealed class BypassBuilder : BaseBuilder<BypassService> {
	BypassBuilder(IBuilder? parent) : base(parent) { }
	public BypassBuilder() : this(null) { }

	public override BypassService Build() => new BypassService();

	protected override IBuilder InternalCreate() => this;
}