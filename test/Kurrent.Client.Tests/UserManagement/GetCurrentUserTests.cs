using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class GetCurrentUserTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[Fact]
	public async Task returns_the_current_user() {
		var user = await Fixture.Users.GetCurrentUserAsync(TestCredentials.Root);
		user.LoginName.ShouldBe(TestCredentials.Root.Username);
	}
}
