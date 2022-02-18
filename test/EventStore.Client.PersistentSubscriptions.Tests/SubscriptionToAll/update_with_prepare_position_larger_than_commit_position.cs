using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_with_prepare_position_larger_than_commit_position
		: IClassFixture<update_with_prepare_position_larger_than_commit_position.Fixture> {
		public update_with_prepare_position_larger_than_commit_position(Fixture fixture) {
			_fixture = fixture;
		}


		private const string Group = "existing";

		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}

		[Fact]
		public Task fails_with_argument_out_of_range_exception() =>
			Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
				_fixture.Client.UpdateToAllAsync(Group,
					new PersistentSubscriptionSettings(
						startFrom: new Position(0, 1)),
					userCredentials: TestCredentials.Root));
	}
}
