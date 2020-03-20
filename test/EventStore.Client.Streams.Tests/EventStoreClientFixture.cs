#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase<EventStoreClient> {
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
		}

		protected override EventStoreClient CreateClient(EventStoreClientSettings settings)
			=> new EventStoreClient(settings);
	}
}
