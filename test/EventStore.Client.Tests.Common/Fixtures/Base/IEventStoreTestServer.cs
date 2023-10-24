namespace EventStore.Client; 

public interface IEventStoreTestServer : IAsyncDisposable {
    Task StartAsync(CancellationToken cancellationToken = default);
    void Stop();
}