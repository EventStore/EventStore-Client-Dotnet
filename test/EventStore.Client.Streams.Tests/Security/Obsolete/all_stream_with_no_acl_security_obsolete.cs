namespace EventStore.Client.Streams.Tests.Obsolete;

[Trait("Category", "Security")]
[Obsolete("Will be removed in future release when older subscriptions APIs are removed from the client")]
public class all_stream_with_no_acl_security_obsolete(ITestOutputHelper output, all_stream_with_no_acl_security_obsolete.CustomFixture fixture) : EventStoreTests<all_stream_with_no_acl_security_obsolete.CustomFixture>(output, fixture) {
	[Fact]
	public async Task reading_and_subscribing_is_not_allowed_when_no_credentials_are_passed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward());
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward());
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture_obsolete.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAllObsolete());
	}

	[Fact]
	public async Task reading_and_subscribing_is_not_allowed_for_usual_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward(TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward(TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture_obsolete.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAllObsolete(TestCredentials.TestUser1));
	}

	[Fact]
	public async Task reading_and_subscribing_is_allowed_for_admin_user() {
		await Fixture.ReadAllForward(TestCredentials.TestAdmin);
		await Fixture.ReadAllBackward(TestCredentials.TestAdmin);
		await Fixture.ReadMeta(SecurityFixture_obsolete.AllStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToAllObsolete(TestCredentials.TestAdmin);
	}

	public class CustomFixture : SecurityFixture_obsolete {
		protected override async Task Given() {
			await base.Given();

			await Streams.SetStreamMetadataAsync(AllStream, StreamState.Any, new(), userCredentials: TestCredentials.Root);
		}
	}
}