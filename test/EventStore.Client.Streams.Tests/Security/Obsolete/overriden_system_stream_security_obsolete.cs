namespace EventStore.Client.Streams.Tests.Obsolete;

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class overriden_system_stream_security_obsolete(ITestOutputHelper output, overriden_system_stream_security_obsolete.CustomFixture fixture) : EventStoreTests<overriden_system_stream_security_obsolete.CustomFixture>(output, fixture) {
	[Fact]
	public async Task operations_on_system_stream_succeed_for_authorized_user() {
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

	[Fact]
	public async Task operations_on_system_stream_fail_for_not_authorized_user() {
		var stream = $"${Fixture.GetStreamName()}";
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.ReadStreamBackward(stream, TestCredentials.TestUser2)
		);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(stream, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task operations_on_system_stream_fail_for_anonymous_user() {
		var stream = $"${Fixture.GetStreamName()}";
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStreamObsolete(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(stream));
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
				systemStreamAcl: new("user1", "user1", "user1", "user1", "user1"),
				userStreamAcl: default
			);

			return Streams.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}
