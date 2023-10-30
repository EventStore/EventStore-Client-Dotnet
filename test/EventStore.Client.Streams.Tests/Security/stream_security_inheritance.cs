namespace EventStore.Client.Streams.Tests.Security; 

public class stream_security_inheritance : IClassFixture<stream_security_inheritance.Fixture> {
	readonly Fixture _fixture;

	public stream_security_inheritance(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task acl_inheritance_is_working_properly_on_user_streams() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-no-acl"));
		await _fixture.AppendStream("user-no-acl", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("user-no-acl", TestCredentials.TestUser2)
		);

		await _fixture.AppendStream("user-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-w-diff"));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("user-w-diff", TestCredentials.TestUser1)
		);

		await _fixture.AppendStream("user-w-diff", TestCredentials.TestUser2);
		await _fixture.AppendStream("user-w-diff", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-w-multiple"));
		await _fixture.AppendStream("user-w-multiple", TestCredentials.TestUser1);
		await _fixture.AppendStream("user-w-multiple", TestCredentials.TestUser2);
		await _fixture.AppendStream("user-w-multiple", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("user-w-restricted")
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("user-w-restricted", TestCredentials.TestUser1)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("user-w-restricted", TestCredentials.TestUser2)
		);

		await _fixture.AppendStream("user-w-restricted", TestCredentials.TestAdmin);

		await _fixture.AppendStream("user-w-all", TestCredentials.TestUser1);
		await _fixture.AppendStream("user-w-all", TestCredentials.TestUser2);
		await _fixture.AppendStream("user-w-all", TestCredentials.TestAdmin);

		await _fixture.ReadEvent("user-no-acl", TestCredentials.TestUser1);
		await _fixture.ReadEvent("user-no-acl", TestCredentials.TestUser2);
		await _fixture.ReadEvent("user-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent("user-r-restricted"));
		await _fixture.AppendStream("user-r-restricted", TestCredentials.TestUser1);
		await _fixture.ReadEvent("user-r-restricted", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.ReadEvent("user-r-restricted", TestCredentials.TestUser2)
		);

		await _fixture.ReadEvent("user-r-restricted", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task acl_inheritance_is_working_properly_on_user_streams_when_not_authenticated() {
		await _fixture.AppendStream("user-w-all");

		// make sure the stream exists before trying to read it without authentication
		await _fixture.AppendStream("user-no-acl", TestCredentials.TestAdmin);
		await _fixture.ReadEvent("user-no-acl");
	}

	[Fact]
	public async Task acl_inheritance_is_working_properly_on_system_streams() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-no-acl"));
		await _fixture.AppendStream("$sys-no-acl", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("$sys-no-acl", TestCredentials.TestUser2)
		);

		await _fixture.AppendStream("$sys-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-w-diff"));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("$sys-w-diff", TestCredentials.TestUser1)
		);

		await _fixture.AppendStream("$sys-w-diff", TestCredentials.TestUser2);
		await _fixture.AppendStream("$sys-w-diff", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-w-multiple"));
		await _fixture.AppendStream("$sys-w-multiple", TestCredentials.TestUser1);
		await _fixture.AppendStream("$sys-w-multiple", TestCredentials.TestUser2);
		await _fixture.AppendStream("$sys-w-multiple", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("$sys-w-restricted")
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("$sys-w-restricted", TestCredentials.TestUser1)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.AppendStream("$sys-w-restricted", TestCredentials.TestUser2)
		);

		await _fixture.AppendStream("$sys-w-restricted", TestCredentials.TestAdmin);

		await _fixture.AppendStream("$sys-w-all", TestCredentials.TestUser1);
		await _fixture.AppendStream("$sys-w-all", TestCredentials.TestUser2);
		await _fixture.AppendStream("$sys-w-all", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent("$sys-no-acl"));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.ReadEvent("$sys-no-acl", TestCredentials.TestUser1)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				_fixture.ReadEvent("$sys-no-acl", TestCredentials.TestUser2)
		);

		await _fixture.ReadEvent("$sys-no-acl", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task acl_inheritance_is_working_properly_on_system_streams_when_not_authenticated() => await _fixture.AppendStream("$sys-w-all");

	public class Fixture : SecurityFixture {
		protected override async Task When() {
			var settings = new SystemSettings(
				new(writeRole: "user1"),
				new(writeRole: "user1")
			);

			await Client.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);

			await Client.SetStreamMetadataAsync(
				"user-no-acl",
				StreamState.NoStream,
				new(),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"user-w-diff",
				StreamState.NoStream,
				new(acl: new(writeRole: "user2")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"user-w-multiple",
				StreamState.NoStream,
				new(acl: new(writeRoles: new[] { "user1", "user2" })),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"user-w-restricted",
				StreamState.NoStream,
				new(acl: new(writeRoles: new string[0])),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"user-w-all",
				StreamState.NoStream,
				new(acl: new(writeRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"user-r-restricted",
				StreamState.NoStream,
				new(acl: new("user1")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"$sys-no-acl",
				StreamState.NoStream,
				new(),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"$sys-w-diff",
				StreamState.NoStream,
				new(acl: new(writeRole: "user2")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"$sys-w-multiple",
				StreamState.NoStream,
				new(acl: new(writeRoles: new[] { "user1", "user2" })),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"$sys-w-restricted",
				StreamState.NoStream,
				new(acl: new(writeRoles: new string[0])),
				userCredentials: TestCredentials.TestAdmin
			);

			await Client.SetStreamMetadataAsync(
				"$sys-w-all",
				StreamState.NoStream,
				new(acl: new(writeRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin
			);
		}
	}
}