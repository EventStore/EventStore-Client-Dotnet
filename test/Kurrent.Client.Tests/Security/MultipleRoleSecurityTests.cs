using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Security")]
public class MultipleRoleSecurityTests(ITestOutputHelper output, MultipleRoleSecurityTests.CustomFixture fixture)
	: KurrentTemporaryTests<MultipleRoleSecurityTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task multiple_roles_are_handled_correctly() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("usr-stream"));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => Fixture.ReadEvent("usr-stream", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => Fixture.ReadEvent("usr-stream", TestCredentials.TestUser2));
		await Assert.ThrowsAsync<StreamNotFoundException>(() => Fixture.ReadEvent("usr-stream", TestCredentials.TestAdmin));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("usr-stream"));
		await Fixture.AppendStream("usr-stream", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("usr-stream", TestCredentials.TestUser2));
		await Fixture.AppendStream("usr-stream", TestCredentials.TestAdmin);

		await Fixture.DeleteStream("usr-stream2", TestCredentials.TestUser1);
		await Fixture.DeleteStream("usr-stream3", TestCredentials.TestUser2);
		await Fixture.DeleteStream("usr-stream4", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task multiple_roles_are_handled_correctly_without_authentication() =>
		await Fixture.DeleteStream("usr-stream1");

	public class CustomFixture : SecurityFixture {
		protected override async Task When() {
			var settings = new SystemSettings(
				new(
					new[] { "user1", "user2" },
					new[] { "$admins", "user1" },
					new[] { "user1", SystemRoles.All }
				)
			);

			await Streams.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}
