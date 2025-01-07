using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Security")]
public class SystemStreamSecurityTests(ITestOutputHelper output, SecurityFixture fixture) : KurrentTemporaryTests<SecurityFixture>(output, fixture) {
	[Fact]
	public async Task operations_on_system_stream_with_no_acl_set_fail_for_non_admin() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("$system-no-acl", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta("$system-no-acl", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream("$system-no-acl", TestCredentials.TestUser1));
	}

	[Fact]
	public async Task operations_on_system_stream_with_no_acl_set_succeed_for_admin() {
		await Fixture.AppendStream("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.ReadEvent("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.ReadMeta("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.WriteMeta("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.SubscribeToStream("$system-no-acl", TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_fail_for_not_authorized_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() => Fixture.WriteMeta(SecurityFixture.SystemAclStream, TestCredentials.TestUser2, TestCredentials.TestUser1.Username)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(SecurityFixture.SystemAclStream, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_succeed_for_that_user() {
		await Fixture.AppendStream(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(SecurityFixture.SystemAclStream, TestCredentials.TestUser1, TestCredentials.TestUser1.Username);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAclStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin, TestCredentials.TestUser1.Username);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAclStream, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_admins_fail_for_usual_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.ReadStreamBackward(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.WriteMeta(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1, SystemRoles.Admins)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(SecurityFixture.SystemAdminStream, TestCredentials.TestUser1));
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_admins_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin, SystemRoles.Admins);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAdminStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_not_authenticated_user() {
		await Fixture.AppendStream(SecurityFixture.SystemAllStream);
		await Fixture.ReadEvent(SecurityFixture.SystemAllStream);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAllStream);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAllStream);

		await Fixture.ReadMeta(SecurityFixture.SystemAllStream);
		await Fixture.WriteMeta(SecurityFixture.SystemAllStream, role: SystemRoles.All);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAllStream);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_usual_user() {
		await Fixture.AppendStream(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(SecurityFixture.SystemAllStream, TestCredentials.TestUser1, SystemRoles.All);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAllStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin, SystemRoles.All);

		await Fixture.SubscribeToStream(SecurityFixture.SystemAllStream, TestCredentials.TestAdmin);
	}
}
