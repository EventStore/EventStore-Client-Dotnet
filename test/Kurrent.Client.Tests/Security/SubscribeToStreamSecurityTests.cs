using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Security")]
public class SubscribeToStreamSecurityTests(ITestOutputHelper output, SecurityFixture fixture)
	: KurrentTemporaryTests<SecurityFixture>(output, fixture) {
	[Fact]
	public async Task subscribing_to_stream_with_not_existing_credentials_is_not_authenticated() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStream(SecurityFixture.ReadStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_stream_with_no_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(SecurityFixture.ReadStream));

	[Fact]
	public async Task subscribing_to_stream_with_not_authorized_user_credentials_is_denied() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(SecurityFixture.ReadStream, TestCredentials.TestUser2));

	[Fact]
	public async Task reading_stream_with_authorized_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture.ReadStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStream(SecurityFixture.ReadStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task reading_stream_with_admin_user_credentials_succeeds() {
		await Fixture.AppendStream(SecurityFixture.ReadStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStream(SecurityFixture.ReadStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream);
		await Fixture.SubscribeToStream(SecurityFixture.NoAclStream);
	}

	[Fact]
	public async Task subscribing_to_no_acl_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStream(SecurityFixture.NoAclStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStream(SecurityFixture.NoAclStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStream(SecurityFixture.NoAclStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task subscribing_to_no_acl_stream_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStream(SecurityFixture.NoAclStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task subscribing_to_all_access_normal_stream_succeeds_when_no_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream);
		await Fixture.SubscribeToStream(SecurityFixture.NormalAllStream);
	}

	[Fact]
	public async Task
		subscribing_to_all_access_normal_stream_is_not_authenticated_when_not_existing_credentials_are_passed() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.SubscribeToStream(SecurityFixture.NormalAllStream, TestCredentials.TestBadUser));

	[Fact]
	public async Task subscribing_to_all_access_normal_stream_succeeds_when_any_existing_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser1);
		await Fixture.SubscribeToStream(SecurityFixture.NormalAllStream, TestCredentials.TestUser2);
	}

	[Fact]
	public async Task subscribing_to_all_access_normal_streamm_succeeds_when_admin_user_credentials_are_passed() {
		await Fixture.AppendStream(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToStream(SecurityFixture.NormalAllStream, TestCredentials.TestAdmin);
	}
}
