using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Operations")]
public class ShutdownNodeAuthenticationTests(ITestOutputHelper output, ShutdownNodeAuthenticationTests.CustomFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeAuthenticationTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.Operations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
