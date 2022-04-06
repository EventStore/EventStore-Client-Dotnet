using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class deleting_filtered
		: IClassFixture<deleting_filtered.Fixture> {

		private const string Group = "to-be-deleted";
		private readonly Fixture _fixture;

		public deleting_filtered(Fixture fixture) {
			_fixture = fixture;
		}

		[SupportsPSToAll.Fact]
		public async Task the_completion_succeeds() {
			await _fixture.Client.DeleteToAllAsync(Group, userCredentials: TestCredentials.Root);
		}

		public class Fixture : EventStoreClientFixture {
			protected override async Task Given() {
				await Client.CreateToAllAsync(
					Group,
					EventTypeFilter.Prefix("prefix-filter-"),
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
