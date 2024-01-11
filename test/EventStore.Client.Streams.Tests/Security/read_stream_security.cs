namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Security")]
public class read_stream_security(ITestOutputHelper output, SecurityFixture fixture) : EventStoreTests<SecurityFixture>(output, fixture) { 
	[Fact]
	public async Task reading_stream_with_not_existing_credentials_is_not_authenticated() {
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadEvent(SecurityFixture.ReadStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamForward(SecurityFixture.ReadStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamBackward(SecurityFixture.ReadStream, TestCredentials.TestBadUser));
	}

	[Fact]
	public async Task reading_stream_with_no_credentials_is_denied() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture.ReadStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture.ReadStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(SecurityFixture.ReadStream));
	}

	[Fact]
	public async Task reading_stream_with_not_authorized_user_credentials_is_denied() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture.ReadStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture.ReadStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(SecurityFixture.ReadStream, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task reading_stream_with_authorized_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture.ReadStream, TestCredentials.TestUser1);
		
		await Fixture.ReadEvent(SecurityFixture.ReadStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture.ReadStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture.ReadStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task reading_stream_with_admin_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture.ReadStream, TestCredentials.TestAdmin);

		await Fixture.ReadEvent(SecurityFixture.ReadStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.ReadStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.ReadStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task reading_no_acl_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream);

		await Fixture.ReadEvent(SecurityFixture.NoAclStream);
		await Fixture.ReadStreamForward(SecurityFixture.NoAclStream);
		await Fixture.ReadStreamBackward(SecurityFixture.NoAclStream);
	}

	[Fact]
	public async Task reading_no_acl_stream_is_not_authenticated_when_not_existing_credentials_are_passed() {
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadEvent(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamForward(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamBackward(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));
	}

	[Fact]
	public async Task reading_no_acl_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestUser1);

		await Fixture.ReadEvent(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
		await Fixture.ReadStreamForward(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
		await Fixture.ReadStreamBackward(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task reading_no_acl_stream_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task reading_all_access_normal_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream);
		await Fixture.ReadEvent(SecurityFixture.NormalAllStream);
		await Fixture.ReadStreamForward(SecurityFixture.NormalAllStream);
		await Fixture.ReadStreamBackward(SecurityFixture.NormalAllStream);
	}

	[Fact]
	public async Task reading_all_access_normal_stream_is_not_authenticated_when_not_existing_credentials_are_passed() {
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadEvent(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamForward(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadStreamBackward(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));
	}

	[Fact]
	public async Task reading_all_access_normal_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
		await Fixture.ReadStreamForward(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
		await Fixture.ReadStreamBackward(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task reading_all_access_normal_stream_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
	}
}