using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	public class EventStoreTestServerExternal : IEventStoreTestServer {
		public EventStoreTestServerExternal() {
		}

		public ValueTask DisposeAsync() {
			return new ValueTask(Task.CompletedTask);
		}

		public Task StartAsync(CancellationToken cancellationToken = default) {
			return Task.CompletedTask;
		}

		public void Stop() {
		}
	}
}
