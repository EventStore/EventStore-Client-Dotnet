namespace EventStore.Client.Streams.Tests.Security; 

public class delete_stream_security : IClassFixture<SecurityFixture> {
	public delete_stream_security(ITestOutputHelper output, SecurityFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	SecurityFixture Fixture { get; }

	[Fact]
	public async Task delete_of_all_is_never_allowed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream, TestCredentials.TestAdmin));
	}

	[AnonymousAccess.Fact]
	public async Task deleting_normal_no_acl_stream_with_no_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(new());
		await Fixture.DeleteStream(streamId);
	}

	[Fact]
	public async Task deleting_normal_no_acl_stream_with_existing_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(new());
		await Fixture.DeleteStream(streamId, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task deleting_normal_no_acl_stream_with_admin_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(new());
		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task deleting_normal_user_stream_with_no_user_is_not_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: TestCredentials.TestUser1.Username)));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId));
	}

	[Fact]
	public async Task deleting_normal_user_stream_with_not_authorized_user_is_not_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: TestCredentials.TestUser1.Username)));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task deleting_normal_user_stream_with_authorized_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: TestCredentials.TestUser1.Username)));

		await Fixture.DeleteStream(streamId, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task deleting_normal_user_stream_with_admin_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: TestCredentials.TestUser1.Username)));

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task deleting_normal_admin_stream_with_no_user_is_not_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.Admins)));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId));
	}

	[Fact]
	public async Task deleting_normal_admin_stream_with_existing_user_is_not_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.Admins)));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId, TestCredentials.TestUser1));
	}

	[Fact]
	public async Task deleting_normal_admin_stream_with_admin_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.Admins)));

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task deleting_normal_all_stream_with_no_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.All)));

		await Fixture.DeleteStream(streamId);
	}

	[Fact]
	public async Task deleting_normal_all_stream_with_existing_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.All)));

		await Fixture.DeleteStream(streamId, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task deleting_normal_all_stream_with_admin_user_is_allowed() {
		var streamId =
			await Fixture.CreateStreamWithMeta(new(acl: new(deleteRole: SystemRoles.All)));

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	// $-stream

	[Fact]
	public async Task deleting_system_no_acl_stream_with_no_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new()
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId));
	}

	[Fact]
	public async Task deleting_system_no_acl_stream_with_existing_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new()
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId, TestCredentials.TestUser1));
	}

	[Fact]
	public async Task deleting_system_no_acl_stream_with_admin_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new()
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task deleting_system_user_stream_with_no_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: TestCredentials.TestUser1.Username))
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId));
	}

	[Fact]
	public async Task deleting_system_user_stream_with_not_authorized_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: TestCredentials.TestUser1.Username))
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task deleting_system_user_stream_with_authorized_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: TestCredentials.TestUser1.Username))
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task deleting_system_user_stream_with_admin_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: TestCredentials.TestUser1.Username))
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task deleting_system_admin_stream_with_no_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.Admins))
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId));
	}

	[Fact]
	public async Task deleting_system_admin_stream_with_existing_user_is_not_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.Admins))
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(streamId, TestCredentials.TestUser1));
	}

	[Fact]
	public async Task deleting_system_admin_stream_with_admin_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.Admins))
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task deleting_system_all_stream_with_no_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.All))
		);

		await Fixture.DeleteStream(streamId);
	}

	[Fact]
	public async Task deleting_system_all_stream_with_existing_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.All))
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task deleting_system_all_stream_with_admin_user_is_allowed() {
		var streamId = await Fixture.CreateStreamWithMeta(
			streamId: $"${Fixture.GetStreamName()}",
			metadata: new(acl: new(deleteRole: SystemRoles.All))
		);

		await Fixture.DeleteStream(streamId, TestCredentials.TestAdmin);
	}
}