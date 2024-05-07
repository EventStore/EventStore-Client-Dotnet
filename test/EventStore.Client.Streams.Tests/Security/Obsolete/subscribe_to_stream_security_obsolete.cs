namespace EventStore.Client.Streams.Tests.Obsolete; 

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class subscribe_to_stream_security_obsolete(ITestOutputHelper output, SecurityFixture_obsolete fixture) : EventStoreTests<SecurityFixture_obsolete>(output, fixture) { 
	[Fact]
	public async Task subscribing_to_stream_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.ReadStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_stream_with_no_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.ReadStream));

	[Fact]
	public async Task subscribing_to_stream_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.ReadStream, TestCredentials.TestUser2));

	[Fact]
	public async Task reading_stream_with_authorized_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture_obsolete.ReadStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.ReadStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task reading_stream_with_admin_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture_obsolete.ReadStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.ReadStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NoAclStream);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NoAclStream);
	}

	[Fact]
	public async Task subscribing_to_no_acl_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NoAclStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task subscribing_to_all_access_normal_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NormalAllStream);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NormalAllStream);
	}

	[Fact]
	public async Task subscribing_to_all_access_normal_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_all_access_normal_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task subscribing_to_all_access_normal_streamm_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.NormalAllStream, TestCredentials.TestAdmin);
	}
}
