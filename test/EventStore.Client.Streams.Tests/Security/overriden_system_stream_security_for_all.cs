namespace EventStore.Client.Streams.Tests.Security; 

public class overriden_system_stream_security_for_all
	: IClassFixture<overriden_system_stream_security_for_all.Fixture> {
	readonly Fixture _fixture;

	public overriden_system_stream_security_for_all(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task operations_on_system_stream_succeeds_for_user() {
		var stream = $"${_fixture.GetStreamName()}";
		await _fixture.AppendStream(stream, TestCredentials.TestUser1);
		await _fixture.ReadEvent(stream, TestCredentials.TestUser1);
		await _fixture.ReadStreamForward(stream, TestCredentials.TestUser1);
		await _fixture.ReadStreamBackward(stream, TestCredentials.TestUser1);

		await _fixture.ReadMeta(stream, TestCredentials.TestUser1);
		await _fixture.WriteMeta(stream, TestCredentials.TestUser1);

		await _fixture.SubscribeToStream(stream, TestCredentials.TestUser1);

		await _fixture.DeleteStream(stream, TestCredentials.TestUser1);
	}

	[AnonymousAccess.Fact]
	public async Task operations_on_system_stream_fail_for_anonymous_user() {
		var stream = $"${_fixture.GetStreamName()}";
		await _fixture.AppendStream(stream);
		await _fixture.ReadEvent(stream);
		await _fixture.ReadStreamForward(stream);
		await _fixture.ReadStreamBackward(stream);

		await _fixture.ReadMeta(stream);
		await _fixture.WriteMeta(stream);

		await _fixture.SubscribeToStream(stream);

		await _fixture.DeleteStream(stream);
	}

	[Fact]
	public async Task operations_on_system_stream_succeed_for_admin() {
		var stream = $"${_fixture.GetStreamName()}";
		await _fixture.AppendStream(stream, TestCredentials.TestAdmin);

		await _fixture.ReadEvent(stream, TestCredentials.TestAdmin);
		await _fixture.ReadStreamForward(stream, TestCredentials.TestAdmin);
		await _fixture.ReadStreamBackward(stream, TestCredentials.TestAdmin);

		await _fixture.ReadMeta(stream, TestCredentials.TestAdmin);
		await _fixture.WriteMeta(stream, TestCredentials.TestAdmin);

		await _fixture.SubscribeToStream(stream, TestCredentials.TestAdmin);

		await _fixture.DeleteStream(stream, TestCredentials.TestAdmin);
	}

	public class Fixture : SecurityFixture {
		protected override Task When() {
			var settings = new SystemSettings(
				systemStreamAcl: new(
					SystemRoles.All,
					SystemRoles.All,
					SystemRoles.All,
					SystemRoles.All,
					SystemRoles.All
				)
			);

			return Client.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}