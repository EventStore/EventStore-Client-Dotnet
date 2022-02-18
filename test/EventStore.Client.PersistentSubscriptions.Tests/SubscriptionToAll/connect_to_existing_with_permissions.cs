using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class connect_to_existing_with_permissions
		: IClassFixture<connect_to_existing_with_permissions.Fixture> {
		private const string Group = "connectwithpermissions";


		private readonly Fixture _fixture;

		public connect_to_existing_with_permissions(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task the_subscription_succeeds() {
			var dropped = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
			using var subscription = await _fixture.Client.SubscribeToAllAsync(Group,
				delegate { return Task.CompletedTask; }, (s, reason, ex) => dropped.TrySetResult((reason, ex)),
				userCredentials: TestCredentials.Root).WithTimeout();
			Assert.NotNull(subscription);

			await Assert.ThrowsAsync<TimeoutException>(() => dropped.Task.WithTimeout());
		}

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() =>
				Client.CreateToAllAsync(
					Group,
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}
	}
}
