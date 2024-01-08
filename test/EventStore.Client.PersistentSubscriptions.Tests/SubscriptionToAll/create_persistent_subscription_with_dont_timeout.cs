namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class create_with_dont_timeout
	: IClassFixture<create_with_dont_timeout.Fixture> {
	readonly Fixture _fixture;

	public create_with_dont_timeout(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public Task the_subscription_is_created_without_error() =>
		_fixture.Client.CreateToAllAsync(
			"dont-timeout",
			new(messageTimeout: TimeSpan.Zero),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}