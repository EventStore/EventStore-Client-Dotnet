using Grpc.Core;

namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class update_existing_with_commit_position_larger_than_last_indexed_position
	: IClassFixture<update_existing_with_commit_position_larger_than_last_indexed_position.Fixture> {
	const string Group = "existing";

	readonly Fixture _fixture;

	public update_existing_with_commit_position_larger_than_last_indexed_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task fails() {
		var ex = await Assert.ThrowsAsync<RpcException>(
			() =>
				_fixture.Client.UpdateToAllAsync(
					Group,
					new(startFrom: new Position(_fixture.LastCommitPosition + 1, _fixture.LastCommitPosition)),
					userCredentials: TestCredentials.Root
				)
		);

		Assert.Equal(StatusCode.Internal, ex.StatusCode);
	}

	public class Fixture : EventStoreClientFixture {
		public ulong LastCommitPosition;

		protected override async Task When() {
			await Client.CreateToAllAsync(
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

			var lastEvent = await StreamsClient.ReadAllAsync(
				Direction.Backwards,
				Position.End,
				1,
				userCredentials: TestCredentials.Root
			).FirstAsync();

			LastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new();
		}

		protected override Task Given() => Task.CompletedTask;
	}
}