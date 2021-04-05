using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class deleting_existing_with_permissions
		: IClassFixture<deleting_existing_with_permissions.Fixture> {

		private readonly Fixture _fixture;

		public deleting_existing_with_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() =>
				Client.CreateToAllAsync("groupname123",
					new PersistentSubscriptionSettings(),
					TestCredentials.Root);
		}

		[Fact]
		public Task the_delete_of_group_succeeds() =>
			_fixture.Client.DeleteToAllAsync("groupname123",
				TestCredentials.Root);
	}
}
