using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests;

public class ShutdownNodeAuthenticationTests(ITestOutputHelper output, ShutdownNodeAuthenticationTests.CustomFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeAuthenticationTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.Operations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
