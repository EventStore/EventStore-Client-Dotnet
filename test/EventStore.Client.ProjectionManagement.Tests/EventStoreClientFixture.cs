using System;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase<EventStoreProjectionManagementClient> {
		public EventStoreUserManagementClient UserManagement { get; }
		public EventStoreClient Streams { get; }

		protected EventStoreClientFixture(EventStoreClientSettings? settings = null) : base(settings,
			new[] {"EVENTSTORE_RUN_PROJECTIONS=ALL", "EVENTSTORE_START_STANDARD_PROJECTIONS=True"}) {
			UserManagement = new EventStoreUserManagementClient(Settings);
			Streams = new EventStoreClient(Settings);
		}

		protected override EventStoreProjectionManagementClient CreateClient(EventStoreClientSettings settings)
			=> new EventStoreProjectionManagementClient(settings);

		protected virtual bool RunStandardProjections => true;

		public override async Task InitializeAsync() {
			await TestServer.Started;
			await UserManagement.CreateUserAsync(TestCredentials.TestUser1.Username, TestCredentials.TestUser1.Username,
				Array.Empty<string>(), TestCredentials.TestUser1.Password, TestCredentials.Root).WithTimeout();
			await StandardProjections.Created(Client).WithTimeout(TimeSpan.FromMinutes(5));

			if (RunStandardProjections) {
				await Task.WhenAll(StandardProjections.Names.Select(name =>
					Client.EnableAsync(name, TestCredentials.Root)));
			}

			await Given().WithTimeout(TimeSpan.FromMinutes(5));
			await When().WithTimeout(TimeSpan.FromMinutes(5));
		}
	}
}
