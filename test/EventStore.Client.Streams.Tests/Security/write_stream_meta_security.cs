namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Security")]
public class write_stream_meta_security(ITestOutputHelper output, SecurityFixture fixture) : EventStoreTests<SecurityFixture>(output, fixture) { 
	[Fact]
	public async Task writing_meta_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(
			() =>
				Fixture.WriteMeta(
					SecurityFixture.MetaWriteStream,
					TestCredentials.TestBadUser,
					TestCredentials.TestUser1.Username
				)
		);

	[Fact]
	public async Task writing_meta_to_stream_with_no_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(SecurityFixture.MetaWriteStream, role: TestCredentials.TestUser1.Username));

	[Fact]
	public async Task writing_meta_to_stream_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() =>
				Fixture.WriteMeta(
					SecurityFixture.MetaWriteStream,
					TestCredentials.TestUser2,
					TestCredentials.TestUser1.Username
				)
		);

	[Fact]
	public async Task writing_meta_to_stream_with_authorized_user_credentials_succeeds() =>
		await Fixture.WriteMeta(
			SecurityFixture.MetaWriteStream,
			TestCredentials.TestUser1,
			TestCredentials.TestUser1.Username
		);

	[Fact]
	public async Task writing_meta_to_stream_with_admin_user_credentials_succeeds() =>
		await Fixture.WriteMeta(
			SecurityFixture.MetaWriteStream,
			TestCredentials.TestAdmin,
			TestCredentials.TestUser1.Username
		);

	[AnonymousAccess.Fact]
	public async Task writing_meta_to_no_acl_stream_succeeds_when_no_credentials_are_passed() => await Fixture.WriteMeta(SecurityFixture.NoAclStream);

	[Fact]
	public async Task writing_meta_to_no_acl_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.WriteMeta(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task writing_meta_to_no_acl_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.WriteMeta(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task writing_meta_to_no_acl_stream_succeeds_when_admin_user_credentials_are_passed() =>
		await Fixture.WriteMeta(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);

	[AnonymousAccess.Fact]
	public async Task writing_meta_to_all_access_normal_stream_succeeds_when_no_credentials_are_passed() =>
		await Fixture.WriteMeta(SecurityFixture.NormalAllStream, role: SystemRoles.All);

	[Fact]
	public async Task writing_meta_to_all_access_normal_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.WriteMeta(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser, SystemRoles.All));

	[Fact]
	public async Task
		writing_meta_to_all_access_normal_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.WriteMeta(SecurityFixture.NormalAllStream, TestCredentials.TestUser1, SystemRoles.All);
		await Fixture.WriteMeta(SecurityFixture.NormalAllStream, TestCredentials.TestUser2, SystemRoles.All);
	}

	[Fact]
	public async Task writing_meta_to_all_access_normal_stream_succeeds_when_admin_user_credentials_are_passed() =>
		await Fixture.WriteMeta(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin, SystemRoles.All);
}