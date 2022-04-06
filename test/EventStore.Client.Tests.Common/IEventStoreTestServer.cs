using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	public interface IEventStoreTestServer : IAsyncDisposable {
		Task StartAsync(CancellationToken cancellationToken = default);
		void Stop();
	}
}
