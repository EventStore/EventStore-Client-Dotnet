namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Security")]
public class subscribe_to_all_security(ITestOutputHelper output, SecurityFixture fixture) : EventStoreTests<SecurityFixture>(output, fixture) { 
	[Fact]
	public async Task subscribing_to_all_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToAllObsolete(TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_all_with_no_credentials_is_denied() => await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAllObsolete());

	[Fact]
	public async Task subscribing_to_all_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAllObsolete(TestCredentials.TestUser2));

	[Fact]
	public async Task subscribing_to_all_with_authorized_user_credentials_succeeds() => await Fixture.SubscribeToAllObsolete(TestCredentials.TestUser1);

	[Fact]
	public async Task subscribing_to_all_with_admin_user_credentials_succeeds() => await Fixture.SubscribeToAllObsolete(TestCredentials.TestAdmin);
}