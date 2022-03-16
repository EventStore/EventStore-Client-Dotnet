using System;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_duplicate
		: IClassFixture<create_duplicate.Fixture> {
		public create_duplicate(Fixture fixture) {
			_fixture = fixture;
		}


		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() =>
				Client.CreateToAllAsync("group32",
					new PersistentSubscriptionSettings(), userCredentials: TestCredentials.Root);
		}

		[SupportsPSToAll.Fact]
		public async Task the_completion_fails() {
			var ex = await Assert.ThrowsAsync<RpcException>(
				() => _fixture.Client.CreateToAllAsync("group32",
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root));
			Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
		}
	}
}
