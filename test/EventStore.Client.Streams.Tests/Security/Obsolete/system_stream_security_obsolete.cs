namespace EventStore.Client.Streams.Tests.Obsolete; 

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class system_stream_security_obsolete(ITestOutputHelper output, SecurityFixture_obsolete fixture) : EventStoreTests<SecurityFixture_obsolete>(output, fixture) { 
	[Fact]
	public async Task operations_on_system_stream_with_no_acl_set_fail_for_non_admin() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("$system-no-acl", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta("$system-no-acl", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta("$system-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete("$system-no-acl", TestCredentials.TestUser1));
	}

	[Fact]
	public async Task operations_on_system_stream_with_no_acl_set_succeed_for_admin() {
		await Fixture.AppendStream("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.ReadEvent("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.ReadMeta("$system-no-acl", TestCredentials.TestAdmin);
		await Fixture.WriteMeta("$system-no-acl", TestCredentials.TestAdmin);

		await Fixture.SubscribeToStreamObsolete("$system-no-acl", TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_fail_for_not_authorized_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2, TestCredentials.TestUser1.Username));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_succeed_for_that_user() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1, TestCredentials.TestUser1.Username);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_usual_user_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin, TestCredentials.TestUser1.Username);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAclStream, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_admins_fail_for_usual_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.WriteMeta(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1, SystemRoles.Admins)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestUser1));
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_admins_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin, SystemRoles.Admins);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAdminStream, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_not_authenticated_user() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAllStream);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAllStream);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAllStream);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAllStream);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAllStream);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAllStream, role: SystemRoles.All);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAllStream);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_usual_user() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1, SystemRoles.All);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task operations_on_system_stream_with_acl_set_to_all_succeed_for_admin() {
		await Fixture.AppendStream(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadEvent(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin, SystemRoles.All);

		await Fixture.SubscribeToStreamObsolete(SecurityFixture_obsolete.SystemAllStream, TestCredentials.TestAdmin);
	}
}
