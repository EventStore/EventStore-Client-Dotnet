using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public interface IEventStoreTestServer : IAsyncDisposable {
		Task StartAsync(CancellationToken cancellationToken = default);
		void Stop();
	}
}
