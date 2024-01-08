namespace EventStore.Client;

public class EventStoreTestServerExternal : IEventStoreTestServer {
	public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public void Stop()                                                    { }

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}