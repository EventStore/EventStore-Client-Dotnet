namespace EventStore.Tests.Fixtures;

public class EsTestNoOpServer : ITestContainer {
	public void Start() { }
	public void Stop() { }
	public void Dispose() { }
}