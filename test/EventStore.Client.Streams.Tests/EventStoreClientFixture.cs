using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreClient Client { get; }
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null,
			IDictionary<string, string>? env = null) : base(settings, env) {
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
