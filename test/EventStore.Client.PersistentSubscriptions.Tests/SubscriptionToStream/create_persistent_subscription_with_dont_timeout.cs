namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToStream;

public class create_with_dont_timeout
	: IClassFixture<create_with_dont_timeout.Fixture> {
	const    string  Stream = nameof(create_with_dont_timeout);
	readonly Fixture _fixture;

	public create_with_dont_timeout(Fixture fixture) => _fixture = fixture;

	[Fact]
	public Task the_subscription_is_created_without_error() =>
		_fixture.Client.CreateToStreamAsync(
			Stream,
			"dont-timeout",
			new(messageTimeout: TimeSpan.Zero),
			userCredentials: TestCredentials.Root
		);

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}