using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_existing_without_permissions
		: IClassFixture<update_existing_without_permissions.Fixture> {

		private const string Group = "existing";
		private readonly Fixture _fixture;

		public update_existing_without_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[SupportsPSToAll.Fact]
		public async Task the_completion_fails_with_access_denied() {
			await Assert.ThrowsAsync<AccessDeniedException>(
				() => _fixture.Client.UpdateToAllAsync(Group,
					new PersistentSubscriptionSettings()));
		}

		public class Fixture : EventStoreClientFixture {
			public Fixture () : base(noDefaultCredentials: true){
			}
			
			protected override async Task Given() {
				await Client.CreateToAllAsync(Group, new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);
			}

			protected override Task When() => Task.CompletedTask;
		}
	}
}
