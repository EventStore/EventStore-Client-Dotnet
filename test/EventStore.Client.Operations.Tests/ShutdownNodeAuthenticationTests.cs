namespace EventStore.Client.Operations.Tests;

public class ShutdownNodeAuthenticationTests : IClassFixture<InsecureClientTestFixture> {
	public ShutdownNodeAuthenticationTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }
	
	[Fact]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.Operations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();
}