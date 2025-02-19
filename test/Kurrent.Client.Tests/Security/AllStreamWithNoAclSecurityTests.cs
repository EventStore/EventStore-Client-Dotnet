using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Security")]
public class AllStreamWithNoAclSecurityTests(ITestOutputHelper output, AllStreamWithNoAclSecurityTests.CustomFixture fixture)
	: KurrentTemporaryTests<AllStreamWithNoAclSecurityTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task write_to_all_is_never_allowed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(SecurityFixture.AllStream, TestCredentials.TestAdmin));
	}

	[Fact]
	public async Task delete_of_all_is_never_allowed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(SecurityFixture.AllStream, TestCredentials.TestAdmin));
	}

	[Fact]
	public async Task reading_and_subscribing_is_not_allowed_when_no_credentials_are_passed() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward());
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward());
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.AllStream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAll());
	}

	[Fact]
	public async Task reading_and_subscribing_is_not_allowed_for_usual_user() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward(TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward(TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(SecurityFixture.AllStream, TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToAll(TestCredentials.TestUser1));
	}

	[Fact]
	public async Task reading_and_subscribing_is_allowed_for_admin_user() {
		await Fixture.ReadAllForward(TestCredentials.TestAdmin);
		await Fixture.ReadAllBackward(TestCredentials.TestAdmin);
		await Fixture.ReadMeta(SecurityFixture.AllStream, TestCredentials.TestAdmin);
		await Fixture.SubscribeToAll(TestCredentials.TestAdmin);
	}

	[Fact]
	public async Task meta_write_is_not_allowed_when_no_credentials_are_passed() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(SecurityFixture.AllStream));

	[Fact]
	public async Task meta_write_is_not_allowed_for_usual_user() =>
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(SecurityFixture.AllStream, TestCredentials.TestUser1));

	[Fact]
	public Task meta_write_is_allowed_for_admin_user() =>
		Fixture.WriteMeta(SecurityFixture.AllStream, TestCredentials.TestAdmin);

	public class CustomFixture : SecurityFixture {
		protected override async Task Given() {
			await base.Given();

			await Streams.SetStreamMetadataAsync(AllStream, StreamState.Any, new(), userCredentials: TestCredentials.Root);
		}
	}
}
