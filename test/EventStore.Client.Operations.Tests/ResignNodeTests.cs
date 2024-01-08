namespace EventStore.Client.Operations.Tests;

public class ResignNodeTests : IClassFixture<InsecureClientTestFixture> {
	public ResignNodeTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }

	[Fact]
	public async Task resign_node_does_not_throw() =>
		await Fixture.Operations
			.ResignNodeAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[Fact]
	public async Task resign_node_without_credentials_throws() =>
		await Fixture.Operations
			.ResignNodeAsync()
			.ShouldThrowAsync<AccessDeniedException>();
}