using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace EventStore.Tests.Fixtures;

public abstract class TestCompositeContainer : ITestContainer {
	ICompositeService Container { get; set; } = null!;

	public void Start() {
		try {
			var builder = Configure();
			Container = builder.Build();
			Container.Start();
		} catch {
			Container.Dispose();
			throw;
		}

		OnContainerStarted();
	}

	public void Stop() {
		OnContainerStop();
		Container.Stop();
	}

	public void Dispose() {
		Stop();
		try {
			Container.Dispose();
		} catch {
			// Ignore
		}
	}

	protected abstract CompositeBuilder Configure();

	protected virtual void OnContainerStarted() { }

	protected virtual void OnContainerStop() { }
}