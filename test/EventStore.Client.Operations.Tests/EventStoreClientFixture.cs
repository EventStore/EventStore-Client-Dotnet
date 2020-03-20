#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase<EventStoreOperationsClient> {
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
		}

		protected override EventStoreOperationsClient CreateClient(EventStoreClientSettings settings)
			=> new EventStoreOperationsClient(settings);
	}
}
