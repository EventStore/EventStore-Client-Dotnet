namespace EventStore.Client.Streams.Tests.Obsolete; 

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class overriden_system_stream_security_for_all_obsolete(ITestOutputHelper output, overriden_system_stream_security_for_all_obsolete.CustomFixture fixture) : EventStoreTests<overriden_system_stream_security_for_all_obsolete.CustomFixture>(output, fixture) {
	[Fact]
	public async Task operations_on_system_stream_succeeds_for_user() {
		var stream = $"${Fixture.GetStreamName()}";
		await Fixture.AppendStream(stream, TestCredentials.TestUser1);
		await Fixture.ReadEvent(stream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(stream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(stream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(stream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(stream, TestCredentials.TestUser1);

		await Fixture.SubscribeToStreamObsolete(stream, TestCredentials.TestUser1);

		await Fixture.DeleteStream(stream, TestCredentials.TestUser1);
	}

	[AnonymousAccess.Fact]
	public async Task operations_on_system_stream_fail_for_anonymous_user() {
		var stream = $"${Fixture.GetStreamName()}";
		await Fixture.AppendStream(stream);
		await Fixture.ReadEvent(stream);
		await Fixture.ReadStreamForward(stream);
		await Fixture.ReadStreamBackward(stream);

		await Fixture.ReadMeta(stream);
		await Fixture.WriteMeta(stream);

		await Fixture.SubscribeToStreamObsolete(stream);

		await Fixture.DeleteStream(stream);
	}

	[Fact]
	public async Task operations_on_system_stream_succeed_for_admin() {
		var stream = $"${Fixture.GetStreamName()}";
		await Fixture.AppendStream(stream, TestCredentials.TestAdmin);

		await Fixture.ReadEvent(stream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(stream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(stream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(stream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(stream, TestCredentials.TestAdmin);

		await Fixture.SubscribeToStreamObsolete(stream, TestCredentials.TestAdmin);

		await Fixture.DeleteStream(stream, TestCredentials.TestAdmin);
	}

	public class CustomFixture : SecurityFixture_obsolete {
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

			return Streams.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}
