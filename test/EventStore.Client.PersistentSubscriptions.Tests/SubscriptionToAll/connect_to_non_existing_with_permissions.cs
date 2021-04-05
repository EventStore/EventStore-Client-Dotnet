using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_non_existing_with_permissions
		: IClassFixture<connect_to_non_existing_with_permissions.Fixture> {

		private const string Group = "foo";

		private readonly Fixture _fixture;

		public connect_to_non_existing_with_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task throws_persistent_subscription_not_found() {
			var ex = await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () => {
				using var _ = await _fixture.Client.SubscribeToAllAsync(
					Group,
					delegate {
						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root);
			}).WithTimeout();

			Assert.Equal(SystemStreams.AllStream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
