using EventStore.Client;
using Kurrent.Client.Tests.TestNode;

namespace Kurrent.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class RestartSubsystemTests(ITestOutputHelper output, RestartSubsystemTests.CustomFixture fixture)
	: KurrentTemporaryTests<RestartSubsystemTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task restart_subsystem_does_not_throw() =>
		await Fixture.Projections.RestartSubsystemAsync(userCredentials: TestCredentials.Root);

	[Fact]
	public async Task restart_subsystem_throws_when_given_no_credentials() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.Projections.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1));

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
