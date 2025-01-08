using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class ListUserTests(ITestOutputHelper output, KurrentPermanentFixture fixture) : KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[Fact]
	public async Task returns_all_created_users() {
		var seed = await Fixture.CreateTestUsers();

		var admin = new UserDetails("admin", "Event Store Administrator", new[] { "$admins" }, false, default);
		var ops   = new UserDetails("ops", "Event Store Operations", new[] { "$ops" }, false, default);

		var expected = new[] { admin, ops }
			.Concat(seed.Select(user => user.Details))
			.ToArray();

		var actual = await Fixture.Users
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual);
	}

	[Fact]
	public async Task returns_all_system_users() {
		var admin = new UserDetails("admin", "Event Store Administrator", new[] { "$admins" }, false, default);
		var ops   = new UserDetails("ops", "Event Store Operations", new[] { "$ops" }, false, default);

		var expected = new[] { admin, ops };

		var actual = await Fixture.Users
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual);
	}
}
