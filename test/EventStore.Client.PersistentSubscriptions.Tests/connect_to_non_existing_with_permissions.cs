using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class connect_to_non_existing_with_permissions
		: IClassFixture<connect_to_non_existing_with_permissions.Fixture> {
		private readonly Fixture _fixture;

		public connect_to_non_existing_with_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task throws_persistent_subscription_not_found() {
			var streamName = _fixture.GetStreamName();
			var ex = await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () => {
				using var _ = await _fixture.Client.SubscribeAsync(
					streamName,
					"foo",
					delegate {
						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root);
			}).WithTimeout();

			Assert.Equal(streamName, ex.StreamName);
			Assert.Equal("foo", ex.GroupName);
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
