namespace EventStore.Client.Operations.Tests;

public class ShutdownNodeTests : IClassFixture<InsecureClientTestFixture> {
	public ShutdownNodeTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }

	[Fact]
	public async Task shutdown_does_not_throw() =>
		await Fixture.Operations.ShutdownAsync(userCredentials: TestCredentials.Root).ShouldNotThrowAsync();
}