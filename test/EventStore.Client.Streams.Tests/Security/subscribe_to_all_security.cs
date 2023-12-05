namespace EventStore.Client.Streams.Tests.Security; 

public class subscribe_to_all_security : IClassFixture<SecurityFixture> {
	public subscribe_to_all_security(ITestOutputHelper output, SecurityFixture fixture) => Fixture = fixture.With(x => x.CaptureTestRun(output));

	SecurityFixture Fixture { get; }

	[Fact]
	public async Task subscribing_to_all_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToAll(TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_all_with_no_credentials_is_denied() => await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAll());

	[Fact]
	public async Task subscribing_to_all_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAll(TestCredentials.TestUser2));

	[Fact]
	public async Task subscribing_to_all_with_authorized_user_credentials_succeeds() => await Fixture.SubscribeToAll(TestCredentials.TestUser1);

	[Fact]
	public async Task subscribing_to_all_with_admin_user_credentials_succeeds() => await Fixture.SubscribeToAll(TestCredentials.TestAdmin);
}