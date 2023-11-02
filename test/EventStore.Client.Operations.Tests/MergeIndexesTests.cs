namespace EventStore.Client.Operations.Tests;

public class MergeIndexesTests : IClassFixture<InsecureClientTestFixture> {
	public MergeIndexesTests(ITestOutputHelper output, InsecureClientTestFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }

	[Fact]
	public async Task merge_indexes_does_not_throw() =>
		await Fixture.Operations
			.MergeIndexesAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[Fact]
	public async Task merge_indexes_without_credentials_throws() =>
		await Fixture.Operations
			.MergeIndexesAsync()
			.ShouldThrowAsync<AccessDeniedException>();
}