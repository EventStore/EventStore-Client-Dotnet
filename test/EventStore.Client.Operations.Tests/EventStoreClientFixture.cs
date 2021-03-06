using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreOperationsClient Client { get; }
		public EventStoreClient StreamsClient { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
			Client = new EventStoreOperationsClient(Settings);
			StreamsClient = new EventStoreClient(Settings);
		}

		protected override async Task OnServerUpAsync() {
			await StreamsClient.WarmUpAsync();
		}

		public override async Task DisposeAsync() {
			await StreamsClient.DisposeAsync();
			await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
