using FlakyTest.XUnit.Attributes;

namespace EventStore.Client.Operations.Tests;

public class ShutdownNodeTests : IClassFixture<InsecureClientTestFixture> {
	public ShutdownNodeTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }

	[Fact]
	public async Task shutdown_does_not_throw() => 
		await Fixture.Operations.ShutdownAsync(userCredentials: TestCredentials.Root).ShouldNotThrowAsync();
	
	[MaybeFixedFact(1)]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.Operations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();
}