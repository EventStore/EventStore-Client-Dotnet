namespace EventStore.Client.Streams.Tests.Security; 

public class multiple_role_security : IClassFixture<multiple_role_security.Fixture> {
	readonly Fixture _fixture;

	public multiple_role_security(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task multiple_roles_are_handled_correctly() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent("usr-stream"));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => _fixture.ReadEvent("usr-stream", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => _fixture.ReadEvent("usr-stream", TestCredentials.TestUser2));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => _fixture.ReadEvent("usr-stream", TestCredentials.TestAdmin));

		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("usr-stream"));
		await _fixture.AppendStream("usr-stream", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream("usr-stream", TestCredentials.TestUser2));
		await _fixture.AppendStream("usr-stream", TestCredentials.TestAdmin);

		await _fixture.DeleteStream("usr-stream2", TestCredentials.TestUser1);
		await _fixture.DeleteStream("usr-stream3", TestCredentials.TestUser2);
		await _fixture.DeleteStream("usr-stream4", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task multiple_roles_are_handled_correctly_without_authentication() => await _fixture.DeleteStream("usr-stream1");

	public class Fixture : SecurityFixture {
		protected override Task When() {
			var settings = new SystemSettings(
				new(
					new[] { "user1", "user2" },
					new[] { "$admins", "user1" },
					new[] { "user1", SystemRoles.All }
				)
			);

			return Client.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}