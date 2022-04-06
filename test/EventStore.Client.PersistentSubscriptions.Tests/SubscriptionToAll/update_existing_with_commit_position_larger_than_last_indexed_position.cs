using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_existing_with_commit_position_larger_than_last_indexed_position
		: IClassFixture<update_existing_with_commit_position_larger_than_last_indexed_position.Fixture> {
		public update_existing_with_commit_position_larger_than_last_indexed_position(Fixture fixture) {
			_fixture = fixture;
		}


		private const string Group = "existing";

		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			public ulong LastCommitPosition;
			protected override async Task When() {
				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(),
					userCredentials: TestCredentials.Root);

				var lastEvent = await StreamsClient.ReadAllAsync(Direction.Backwards, Position.End, 1,
					userCredentials: TestCredentials.Root).FirstAsync();
				LastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new Exception();
			}
			protected override Task Given() => Task.CompletedTask;
		}
		
		[SupportsPSToAll.Fact]
		public async Task fails() {
			var ex = await Assert.ThrowsAsync<RpcException>(() =>
				_fixture.Client.UpdateToAllAsync(Group,
					new PersistentSubscriptionSettings(
						startFrom: new Position(_fixture.LastCommitPosition + 1, _fixture.LastCommitPosition)),
					userCredentials: TestCredentials.Root));
			Assert.Equal(StatusCode.Internal, ex.StatusCode);
		}
	}
}
