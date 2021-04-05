using System;
using System.Linq;
using System.Threading.Tasks;
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
					TestCredentials.Root);

				var lastEvent = await StreamsClient.ReadAllAsync(Direction.Backwards, Position.End, 1,
					userCredentials: TestCredentials.Root).FirstAsync();
				LastCommitPosition = lastEvent.OriginalPosition?.CommitPosition ?? throw new Exception();
			}
			protected override Task Given() => Task.CompletedTask;
		}

		[Fact]
		public Task fails_with_invalid_operation_exception() =>
			Assert.ThrowsAsync<InvalidOperationException>(() =>
				_fixture.Client.UpdateToAllAsync(Group,
					new PersistentSubscriptionSettings(
						startFrom: new Position(_fixture.LastCommitPosition+1, _fixture.LastCommitPosition)),
						TestCredentials.Root));
	}
}
