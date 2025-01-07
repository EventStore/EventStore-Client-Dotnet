using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Security")]
public class WriteStreamSecurityTests : IClassFixture<SecurityFixture> {
	public WriteStreamSecurityTests(ITestOutputHelper output, SecurityFixture fixture) => Fixture = fixture.With(x => x.CaptureTestRun(output));

	SecurityFixture Fixture { get; }

	[Fact]
	public async Task writing_to_all_is_never_allowed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream, TestCredentials.TestAdmin));
	}

	[Fact]
	public async Task writing_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.AppendStream(SecurityFixture.WriteStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task writing_to_stream_with_no_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.WriteStream));

	[Fact]
	public async Task writing_to_stream_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.WriteStream, TestCredentials.TestUser2));

	[Fact]
	public async Task writing_to_stream_with_authorized_user_credentials_succeeds() =>
		await Fixture.AppendStream(SecurityFixture.WriteStream, TestCredentials.TestUser1);

	[Fact]
	public async Task writing_to_stream_with_admin_user_credentials_succeeds() =>
		await Fixture.AppendStream(SecurityFixture.WriteStream, TestCredentials.TestAdmin);

	[AnonymousAccess.Fact]
	public async Task writing_to_no_acl_stream_succeeds_when_no_credentials_are_passed() => await Fixture.AppendStream(SecurityFixture.NoAclStream);

	[Fact]
	public async Task writing_to_no_acl_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task writing_to_no_acl_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task writing_to_no_acl_stream_succeeds_when_any_admin_user_credentials_are_passed() =>
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);

	[AnonymousAccess.Fact]
	public async Task writing_to_all_access_normal_stream_succeeds_when_no_credentials_are_passed() =>
		await Fixture.AppendStream(SecurityFixture.NormalAllStream);

	[Fact]
	public async Task writing_to_all_access_normal_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task writing_to_all_access_normal_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task writing_to_all_access_normal_stream_succeeds_when_any_admin_user_credentials_are_passed() =>
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
}
