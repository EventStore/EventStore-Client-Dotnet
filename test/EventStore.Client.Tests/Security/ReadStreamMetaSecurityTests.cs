using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests;

[Trait("Category", "Security")]
public class ReadStreamMetaSecurityTests(ITestOutputHelper output, SecurityFixture fixture) : KurrentTemporaryTests<SecurityFixture>(output, fixture) {
	[Fact]
	public async Task reading_stream_meta_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadMeta(SecurityFixture.MetaReadStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task reading_stream_meta_with_no_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.MetaReadStream));

	[Fact]
	public async Task reading_stream_meta_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.MetaReadStream, TestCredentials.TestUser2));

	[Fact]
	public async Task reading_stream_meta_with_authorized_user_credentials_succeeds() =>
		await Fixture.ReadMeta(SecurityFixture.MetaReadStream, TestCredentials.TestUser1);

	[Fact]
	public async Task reading_stream_meta_with_admin_user_credentials_succeeds() =>
		await Fixture.ReadMeta(SecurityFixture.MetaReadStream, TestCredentials.TestAdmin);

	[AnonymousAccess.Fact]
	public async Task reading_no_acl_stream_meta_succeeds_when_no_credentials_are_passed() => await Fixture.ReadMeta(SecurityFixture.NoAclStream);

	[Fact]
	public async Task reading_no_acl_stream_meta_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadMeta(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task reading_no_acl_stream_meta_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.ReadMeta(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.ReadMeta(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task reading_no_acl_stream_meta_succeeds_when_admin_user_credentials_are_passed() =>
		await Fixture.ReadMeta(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);

	[AnonymousAccess.Fact]
	public async Task reading_all_access_normal_stream_meta_succeeds_when_no_credentials_are_passed() =>
		await Fixture.ReadMeta(SecurityFixture.NormalAllStream);

	[Fact]
	public async Task
		reading_all_access_normal_stream_meta_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadMeta(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task
		reading_all_access_normal_stream_meta_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.ReadMeta(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.ReadMeta(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task reading_all_access_normal_stream_meta_succeeds_when_admin_user_credentials_are_passed() =>
		await Fixture.ReadMeta(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
}
