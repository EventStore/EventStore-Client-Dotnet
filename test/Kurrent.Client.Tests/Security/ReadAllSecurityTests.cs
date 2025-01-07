using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Security")]
public class ReadAllSecurityTests(ITestOutputHelper output, SecurityFixture fixture) : KurrentTemporaryTests<SecurityFixture>(output, fixture) {
	[Fact]
	public async Task reading_all_with_not_existing_credentials_is_not_authenticated() {
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadAllForward(TestCredentials.TestBadUser));
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.ReadAllBackward(TestCredentials.TestBadUser));
	}

	[Fact]
	public async Task reading_all_with_no_credentials_is_denied() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward());
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward());
	}

	[Fact]
	public async Task reading_all_with_not_authorized_user_credentials_is_denied() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllForward(TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadAllBackward(TestCredentials.TestUser2));
	}

	[Fact]
	public async Task reading_all_with_authorized_user_credentials_succeeds() {
		await Fixture.ReadAllForward(TestCredentials.TestUser1);
		await Fixture.ReadAllBackward(TestCredentials.TestUser1);
	}

	[Fact]
	public async Task reading_all_with_admin_credentials_succeeds() {
		await Fixture.ReadAllForward(TestCredentials.TestAdmin);
		await Fixture.ReadAllBackward(TestCredentials.TestAdmin);
	}
}
