using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreUserManagementClient UserManagementClient { get; }
		public EventStoreClient StreamsClient { get; }
		public EventStoreProjectionManagementClient Client { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings,
			new Dictionary<string, string> {
				["EVENTSTORE_RUN_PROJECTIONS"] = "ALL",
				["EVENTSTORE_START_STANDARD_PROJECTIONS"] = "True"
			}) {
			Client = new EventStoreProjectionManagementClient(Settings);
			UserManagementClient = new EventStoreUserManagementClient(Settings);
			StreamsClient = new EventStoreClient(Settings);
		}

		protected virtual bool RunStandardProjections => true;

		public override async Task InitializeAsync() {
			await TestServer.StartAsync();
			await UserManagementClient.CreateUserWithRetry(TestCredentials.TestUser1.Username!,
				TestCredentials.TestUser1.Username!, Array.Empty<string>(), TestCredentials.TestUser1.Password!,
				TestCredentials.Root).WithTimeout();
			await StandardProjections.Created(Client).WithTimeout(TimeSpan.FromMinutes(5));

			if (RunStandardProjections) {
				await Task.WhenAll(StandardProjections.Names.Select(name =>
					Client.EnableAsync(name, TestCredentials.Root)));
			}

			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}

		public override async Task DisposeAsync() {
			await StreamsClient.DisposeAsync();
			await UserManagementClient.DisposeAsync();
			await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
