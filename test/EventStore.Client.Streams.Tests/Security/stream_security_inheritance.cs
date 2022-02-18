using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.Security {
	public class stream_security_inheritance : IClassFixture<stream_security_inheritance.Fixture> {
		private readonly Fixture _fixture;

		public stream_security_inheritance(Fixture fixture) {
			_fixture = fixture;
		}

		public class Fixture : SecurityFixture {
			protected override async Task When() {
				var settings = new SystemSettings(userStreamAcl: new StreamAcl(writeRole: "user1"),
					systemStreamAcl: new StreamAcl(writeRole: "user1"));
				await Client.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);

				await Client.SetStreamMetadataAsync("user-no-acl", StreamState.NoStream,
					new StreamMetadata(), userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("user-w-diff", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRole: "user2")),
					userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("user-w-multiple", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRoles: new[] {"user1", "user2"})),
					userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("user-w-restricted", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRoles: new string[0])),
					userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("user-w-all", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRole: SystemRoles.All)),
					userCredentials: TestCredentials.TestAdmin);

				await Client.SetStreamMetadataAsync("user-r-restricted", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl("user1")), userCredentials: TestCredentials.TestAdmin);

				await Client.SetStreamMetadataAsync("$sys-no-acl", StreamState.NoStream,
					new StreamMetadata(), userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("$sys-w-diff", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRole: "user2")),
					userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("$sys-w-multiple", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRoles: new[] {"user1", "user2"})),
					userCredentials: TestCredentials.TestAdmin);

				await Client.SetStreamMetadataAsync("$sys-w-restricted", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRoles: new string[0])),
					userCredentials: TestCredentials.TestAdmin);
				await Client.SetStreamMetadataAsync("$sys-w-all", StreamState.NoStream,
					new StreamMetadata(acl: new StreamAcl(writeRole: SystemRoles.All)),
					userCredentials: TestCredentials.TestAdmin);
			}
		}


		[Fact]
		public async Task acl_inheritance_is_working_properly_on_user_streams() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-no-acl"));
			await _fixture.AppendStream("user-no-acl", userCredentials: TestCredentials.TestUser1);
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("user-no-acl", TestCredentials.TestUser2));
			await _fixture.AppendStream("user-no-acl", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-w-diff"));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("user-w-diff", userCredentials: TestCredentials.TestUser1));
			await _fixture.AppendStream("user-w-diff", TestCredentials.TestUser2);
			await _fixture.AppendStream("user-w-diff", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("user-w-multiple"));
			await _fixture.AppendStream("user-w-multiple", userCredentials: TestCredentials.TestUser1);
			await _fixture.AppendStream("user-w-multiple", TestCredentials.TestUser2);
			await _fixture.AppendStream("user-w-multiple", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("user-w-restricted"));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("user-w-restricted", userCredentials: TestCredentials.TestUser1));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("user-w-restricted", TestCredentials.TestUser2));
			await _fixture.AppendStream("user-w-restricted", userCredentials: TestCredentials.TestAdmin);

			await _fixture.AppendStream("user-w-all");
			await _fixture.AppendStream("user-w-all", userCredentials: TestCredentials.TestUser1);
			await _fixture.AppendStream("user-w-all", TestCredentials.TestUser2);
			await _fixture.AppendStream("user-w-all", userCredentials: TestCredentials.TestAdmin);


			await _fixture.ReadEvent("user-no-acl");
			await _fixture.ReadEvent("user-no-acl", userCredentials: TestCredentials.TestUser1);
			await _fixture.ReadEvent("user-no-acl", TestCredentials.TestUser2);
			await _fixture.ReadEvent("user-no-acl", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent("user-r-restricted"));
			await _fixture.AppendStream("user-r-restricted", userCredentials: TestCredentials.TestUser1);
			await _fixture.ReadEvent("user-r-restricted", userCredentials: TestCredentials.TestUser1);
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.ReadEvent("user-r-restricted", TestCredentials.TestUser2));
			await _fixture.ReadEvent("user-r-restricted", userCredentials: TestCredentials.TestAdmin);
		}

		[Fact]
		public async Task acl_inheritance_is_working_properly_on_system_streams() {
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-no-acl"));
			await _fixture.AppendStream("$sys-no-acl", userCredentials: TestCredentials.TestUser1);
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("$sys-no-acl", TestCredentials.TestUser2));
			await _fixture.AppendStream("$sys-no-acl", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-w-diff"));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("$sys-w-diff", userCredentials: TestCredentials.TestUser1));
			await _fixture.AppendStream("$sys-w-diff", TestCredentials.TestUser2);
			await _fixture.AppendStream("$sys-w-diff", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("$sys-w-multiple"));
			await _fixture.AppendStream("$sys-w-multiple", userCredentials: TestCredentials.TestUser1);
			await _fixture.AppendStream("$sys-w-multiple", TestCredentials.TestUser2);
			await _fixture.AppendStream("$sys-w-multiple", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("$sys-w-restricted"));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("$sys-w-restricted", userCredentials: TestCredentials.TestUser1));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.AppendStream("$sys-w-restricted", TestCredentials.TestUser2));
			await _fixture.AppendStream("$sys-w-restricted", userCredentials: TestCredentials.TestAdmin);

			await _fixture.AppendStream("$sys-w-all");
			await _fixture.AppendStream("$sys-w-all", userCredentials: TestCredentials.TestUser1);
			await _fixture.AppendStream("$sys-w-all", TestCredentials.TestUser2);
			await _fixture.AppendStream("$sys-w-all", userCredentials: TestCredentials.TestAdmin);

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent("$sys-no-acl"));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.ReadEvent("$sys-no-acl", userCredentials: TestCredentials.TestUser1));
			await Assert.ThrowsAsync<AccessDeniedException>(() =>
				_fixture.ReadEvent("$sys-no-acl", TestCredentials.TestUser2));
			await _fixture.ReadEvent("$sys-no-acl", userCredentials: TestCredentials.TestAdmin);
		}
	}
}
