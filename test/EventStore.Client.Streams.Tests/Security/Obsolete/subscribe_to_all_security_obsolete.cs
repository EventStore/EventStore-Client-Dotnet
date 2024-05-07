namespace EventStore.Client.Streams.Tests.Obsolete; 

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class subscribe_to_all_security_obsolete(ITestOutputHelper output, SecurityFixture_obsolete fixture) : EventStoreTests<SecurityFixture_obsolete>(output, fixture) { 
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