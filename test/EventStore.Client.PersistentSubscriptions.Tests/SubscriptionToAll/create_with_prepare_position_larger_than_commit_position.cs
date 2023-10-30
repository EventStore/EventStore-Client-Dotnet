namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class create_with_prepare_position_larger_than_commit_position
	: IClassFixture<create_with_prepare_position_larger_than_commit_position.Fixture> {
	readonly Fixture _fixture;

	public create_with_prepare_position_larger_than_commit_position(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public Task fails_with_argument_out_of_range_exception() =>
		Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() =>
				_fixture.Client.CreateToAllAsync(
					"group57",
					new(startFrom: new Position(0, 1)),
					userCredentials: TestCredentials.Root
				)
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}