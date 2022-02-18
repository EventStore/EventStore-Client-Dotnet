using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_max_one_client
		: IClassFixture<connect_to_existing_with_max_one_client.Fixture> {
		private const string Group = "maxoneclient";

		private readonly Fixture _fixture;

		public connect_to_existing_with_max_one_client(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_second_subscription_fails_to_connect() {
			using var first = await _fixture.Client.SubscribeToAllAsync(Group,
				delegate { return Task.CompletedTask; },
				userCredentials: TestCredentials.Root).WithTimeout();

			var ex = await Assert.ThrowsAsync<MaximumSubscribersReachedException>(async () => {
				using var _ = await _fixture.Client.SubscribeToAllAsync(Group,
					delegate { return Task.CompletedTask; },
					userCredentials: TestCredentials.Root);
			}).WithTimeout();

			Assert.Equal(SystemStreams.AllStream, ex.StreamName);
			Assert.Equal(Group, ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() =>
				Client.CreateToAllAsync(
					Group,
					new PersistentSubscriptionSettings(maxSubscriberCount: 1),
					userCredentials: TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}
	}
}
