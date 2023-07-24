using System.Threading.Tasks;

namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreUserManagementClient Client { get; }
		public EventStoreClient StreamsClient { get; }
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null, bool noDefaultCredentials = false)
			: base(settings, noDefaultCredentials: noDefaultCredentials) {
			
			Client = new EventStoreUserManagementClient(Settings);
			StreamsClient = new EventStoreClient(Settings);
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
