using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreOperationsClient Client { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
			Client = new EventStoreOperationsClient(Settings);
		}

		public override Task DisposeAsync() {
			Client?.Dispose();
			return base.DisposeAsync();
		}
	}
}
