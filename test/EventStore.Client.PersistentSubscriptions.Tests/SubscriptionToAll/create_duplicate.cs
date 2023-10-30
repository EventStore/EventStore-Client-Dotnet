using Grpc.Core;

namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class create_duplicate : IClassFixture<create_duplicate.Fixture> {
	readonly Fixture _fixture;

	public create_duplicate(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task the_completion_fails() {
		var ex = await Assert.ThrowsAsync<RpcException>(
			() => _fixture.Client.CreateToAllAsync(
				"group32",
				new(),
				userCredentials: TestCredentials.Root
			)
		);

		Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override Task When() =>
			Client.CreateToAllAsync(
				"group32",
				new(),
				userCredentials: TestCredentials.Root
			);
	}
}