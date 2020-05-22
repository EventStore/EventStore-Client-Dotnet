using System;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStorePersistentSubscriptionsClient Client { get; }
		public EventStoreClient StreamsClient { get; }
		public EventStoreUserManagementClient UserManagementClient { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings) {
			Client = new EventStorePersistentSubscriptionsClient(Settings);
			StreamsClient = new EventStoreClient(Settings);
			UserManagementClient = new EventStoreUserManagementClient(Settings);
		}

		public override async Task InitializeAsync() {
			await TestServer.Start().WithTimeout(TimeSpan.FromMinutes(5));
			await UserManagementClient.CreateUserAsync(TestCredentials.TestUser1.Username!,
				TestCredentials.TestUser1.Username!, Array.Empty<string>(), TestCredentials.TestUser1.Password!,
				TestCredentials.Root);
			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}

		public override Task DisposeAsync() {
			UserManagementClient?.Dispose();
			StreamsClient?.Dispose();
			Client?.Dispose();
			return base.DisposeAsync();
		}
	}
}
