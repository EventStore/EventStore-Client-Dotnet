namespace EventStore.Client.Tests;

public class getting_current_user : IClassFixture<EventStoreFixture> {
	public getting_current_user(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task returns_the_current_user() {
		var user = await Fixture.Users.GetCurrentUserAsync(TestCredentials.Root);
		user.LoginName.ShouldBe(TestCredentials.Root.Username);
	}
}