using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreOperationsClient Client { get; }
		public EventStoreClient StreamsClient { get; }
		public new EventStoreClientSettings Settings => base.Settings;

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null, bool? runInMemory = null)
			: base(settings, noDefaultCredentials: true, env: Env(runInMemory)) {
			
			Client = new EventStoreOperationsClient(Settings);
			StreamsClient = new EventStoreClient(Settings);
		}

		private static IDictionary<string, string>? Env(bool? runInMemory) {
			if (runInMemory == null) {
				return null;
			}

			return new Dictionary<string, string>() {
				{ "EVENTSTORE_MEM_DB", runInMemory.Value.ToString() },
			};
		}

		protected override async Task OnServerUpAsync() {
			await StreamsClient.WarmUpAsync();
			await Client.WarmUpAsync();
		}

		public override async Task DisposeAsync() {
			await StreamsClient.DisposeAsync();
			await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
