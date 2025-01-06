using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests.Operations;

public class ShutdownNodeTests(ITestOutputHelper output, ShutdownNodeTests.NoDefaultCredentialsFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeTests.NoDefaultCredentialsFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_does_not_throw() =>
		await Fixture.Operations.ShutdownAsync(userCredentials: TestCredentials.Root).ShouldNotThrowAsync();

	public class NoDefaultCredentialsFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
