using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreClient Client { get; }
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null,
			Dictionary<string, string>? env = null, bool noDefaultCredentials = false)
			: base(settings, env, noDefaultCredentials) {
			
			Client = new EventStoreClient(Settings);
		}

		protected override async Task OnServerUpAsync() {
			await Client.WarmUpAsync();
		}

		public override async Task DisposeAsync() {
			await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
