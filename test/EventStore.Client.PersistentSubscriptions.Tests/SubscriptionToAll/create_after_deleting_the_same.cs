using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_after_deleting_the_same
		: IClassFixture<create_after_deleting_the_same.Fixture> {
		public create_after_deleting_the_same(Fixture fixture) {
			_fixture = fixture;
		}


		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override async Task When() {
				await Client.CreateToAllAsync("existing",
					new PersistentSubscriptionSettings(), TestCredentials.Root);
				await Client.DeleteToAllAsync("existing",
					TestCredentials.Root);
			}
		}

		[Fact]
		public async Task the_completion_succeeds() =>
			await _fixture.Client.CreateToAllAsync("existing",
				new PersistentSubscriptionSettings(), TestCredentials.Root);
	}
}
