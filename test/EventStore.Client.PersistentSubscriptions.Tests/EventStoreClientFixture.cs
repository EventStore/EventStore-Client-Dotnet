using System;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class
		EventStoreClientFixture : EventStoreClientFixtureBase<EventStorePersistentSubscriptionsClient> {
		public EventStoreClient StreamsClient { get; }
		public EventStoreUserManagementClient UserManagementClient { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
			StreamsClient = new EventStoreClient(Settings);
			UserManagementClient = new EventStoreUserManagementClient(Settings);
		}

		protected override EventStorePersistentSubscriptionsClient CreateClient(EventStoreClientSettings settings)
			=> new EventStorePersistentSubscriptionsClient(settings);

		public override async Task InitializeAsync() {
			await TestServer.Started.WithTimeout(TimeSpan.FromMinutes(5));
			await UserManagementClient.CreateUserAsync(TestCredentials.TestUser1.Username,
				TestCredentials.TestUser1.Username, Array.Empty<string>(), TestCredentials.TestUser1.Password,
				TestCredentials.Root);
			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}
	}
}
