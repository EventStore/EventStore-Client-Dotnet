using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_existing_filtered
		: IClassFixture<update_existing_filtered.Fixture> {

		private const string Group = "existing-filtered";
		private readonly Fixture _fixture;

		public update_existing_filtered(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_completion_succeeds() {
			await _fixture.Client.UpdateToAllAsync(
				Group,
				new PersistentSubscriptionSettings(resolveLinkTos: true),
				userCredentials: TestCredentials.Root);
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
