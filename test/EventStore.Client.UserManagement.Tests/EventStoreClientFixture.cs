#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase<EventStoreUserManagementClient> {
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
		}

		protected override EventStoreUserManagementClient CreateClient(EventStoreClientSettings settings)
			=> new EventStoreUserManagementClient(settings);
	}
}
